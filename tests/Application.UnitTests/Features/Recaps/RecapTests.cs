using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.BackgroundJobs;
using Sportner.Application.Features.Recaps;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Sports;
using Sportner.Domain.Users;
using DomainEvent = Sportner.Domain.Events.Event;

namespace Sportner.Application.UnitTests.Features.Recaps;

public sealed class RecapTests
{
    // 4 Aralık 2026 12:00 İstanbul; varsayılan özet Kasım.
    private static readonly DateTimeOffset Now = new(2026, 12, 4, 9, 0, 0, TimeSpan.Zero);
    private const string Maltepe = "Cd. No:1, 34841 Maltepe/İstanbul, Türkiye";

    [Fact]
    public async Task Recap_CountsLastMonth_AndPicksOnlyVisiblePartner()
    {
        await using var db = InMemoryDb.Create();
        var (users, sport) = Seed(db, 3);
        users[2].UserProfile!.UpdateVisibility(false, Now);
        AddPlayedEvent(db, users[1].Id, users[0].Id, sport.Id, new DateTimeOffset(2026, 11, 5, 15, 0, 0, TimeSpan.Zero));
        AddPlayedEvent(db, users[1].Id, users[0].Id, sport.Id, new DateTimeOffset(2026, 11, 12, 15, 0, 0, TimeSpan.Zero));
        for (var day = 14; day <= 16; day++)
        {
            AddPlayedEvent(db, users[2].Id, users[0].Id, sport.Id, new DateTimeOffset(2026, 11, day, 15, 0, 0, TimeSpan.Zero));
        }

        // Ekim'deki maç geçen ay karşılaştırmasına girer, Kasım toplamına girmez.
        AddPlayedEvent(db, users[1].Id, users[0].Id, sport.Id, new DateTimeOffset(2026, 10, 20, 15, 0, 0, TimeSpan.Zero));
        await db.SaveChangesAsync();

        var result = await new GetMonthlyRecapQueryHandler(
                db, new TestCurrentUser(users[0].Id), new FakeTimeProvider(Now))
            .Handle(new GetMonthlyRecapQuery(null), CancellationToken.None);

        var recap = result.Value!;
        recap.Month.Should().Be("2026-11");
        recap.IsReady.Should().BeTrue();
        recap.EventsPlayed.Should().Be(5);
        recap.PreviousMonthEventsPlayed.Should().Be(1);
        recap.MinutesPlayed.Should().Be(300);
        recap.TeammatesCount.Should().Be(2);
        recap.TopPartner!.UserId.Should().Be(users[1].Id);
        recap.TopPartner.EventsTogether.Should().Be(2);
        recap.TopDistrict.Should().Be("Maltepe");
        recap.TopSports.Single().EventsPlayed.Should().Be(5);
    }

    [Fact]
    public async Task Recap_RejectsFutureMonth()
    {
        await using var db = InMemoryDb.Create();
        var result = await new GetMonthlyRecapQueryHandler(
                db, new TestCurrentUser(Guid.NewGuid()), new FakeTimeProvider(Now))
            .Handle(new GetMonthlyRecapQuery("2027-01"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Dispatcher_NotifiesOnlyUsersWithEnoughGames_FromTheThird()
    {
        await using var db = InMemoryDb.Create();
        var (users, sport) = Seed(db, 3);
        AddPlayedEvent(db, users[1].Id, users[0].Id, sport.Id, new DateTimeOffset(2026, 11, 5, 15, 0, 0, TimeSpan.Zero));
        AddPlayedEvent(db, users[1].Id, users[0].Id, sport.Id, new DateTimeOffset(2026, 11, 12, 15, 0, 0, TimeSpan.Zero));
        AddPlayedEvent(db, users[2].Id, users[0].Id, sport.Id, new DateTimeOffset(2026, 11, 14, 15, 0, 0, TimeSpan.Zero));
        await db.SaveChangesAsync();

        var publisher = new RecordingPublisher();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 12, 2, 9, 0, 0, TimeSpan.Zero));
        var dispatcher = new MonthlyRecapDispatcher(
            db, publisher, time, NullLogger<MonthlyRecapDispatcher>.Instance);

        await dispatcher.DispatchAsync();
        publisher.Sent.Should().BeEmpty();

        time.SetUtcNow(Now);
        await dispatcher.DispatchAsync();

        publisher.Sent.Should().BeEquivalentTo(
            [(users[0].Id, NotificationType.MonthlyRecapReady), (users[1].Id, NotificationType.MonthlyRecapReady)]);
    }

    [Fact]
    public async Task YearlyRecap_CountsWholeYear_LongestStreakAndBusiestMonth()
    {
        await using var db = InMemoryDb.Create();
        var (users, sport) = Seed(db, 2);
        // Kasım'da art arda üç hafta (2, 9, 16 Kasım haftaları) + Mart'ta tek etkinlik.
        foreach (var day in new[] { 3, 10, 17 })
        {
            AddPlayedEvent(db, users[1].Id, users[0].Id, sport.Id, new DateTimeOffset(2026, 11, day, 15, 0, 0, TimeSpan.Zero));
        }

        AddPlayedEvent(db, users[1].Id, users[0].Id, sport.Id, new DateTimeOffset(2026, 3, 4, 15, 0, 0, TimeSpan.Zero));
        // Geçen yılın etkinliği sayılmaz.
        AddPlayedEvent(db, users[1].Id, users[0].Id, sport.Id, new DateTimeOffset(2025, 12, 20, 15, 0, 0, TimeSpan.Zero));
        await db.SaveChangesAsync();

        var result = await new GetYearlyRecapQueryHandler(
                db, new TestCurrentUser(users[0].Id), new FakeTimeProvider(new DateTimeOffset(2026, 12, 16, 9, 0, 0, TimeSpan.Zero)))
            .Handle(new GetYearlyRecapQuery(null), CancellationToken.None);

        var recap = result.Value!;
        recap.Year.Should().Be(2026);
        recap.EventsPlayed.Should().Be(4);
        recap.BusiestMonth.Should().Be(11);
        recap.MonthlyCounts[2].Should().Be(1);
        recap.LongestStreakWeeks.Should().Be(3);
        recap.TopPartners.Single().EventsTogether.Should().Be(4);
        recap.Persona.Should().Be("risingStar");
    }

    [Fact]
    public async Task YearlyRecap_DefaultsToLastYear_BeforeTheReveal()
    {
        await using var db = InMemoryDb.Create();
        var result = await new GetYearlyRecapQueryHandler(
                db, new TestCurrentUser(Guid.NewGuid()), new FakeTimeProvider(new DateTimeOffset(2026, 12, 14, 9, 0, 0, TimeSpan.Zero)))
            .Handle(new GetYearlyRecapQuery(null), CancellationToken.None);

        result.Value!.Year.Should().Be(2025);
    }

    [Fact]
    public async Task Dispatcher_SendsYearlyRecapFromDecember15()
    {
        await using var db = InMemoryDb.Create();
        var (users, sport) = Seed(db, 2);
        AddPlayedEvent(db, users[1].Id, users[0].Id, sport.Id, new DateTimeOffset(2026, 4, 5, 15, 0, 0, TimeSpan.Zero));
        AddPlayedEvent(db, users[1].Id, users[0].Id, sport.Id, new DateTimeOffset(2026, 9, 12, 15, 0, 0, TimeSpan.Zero));
        await db.SaveChangesAsync();

        var publisher = new RecordingPublisher();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 12, 14, 9, 0, 0, TimeSpan.Zero));
        var dispatcher = new MonthlyRecapDispatcher(
            db, publisher, time, NullLogger<MonthlyRecapDispatcher>.Instance);

        await dispatcher.DispatchAsync();
        publisher.Sent.Should().BeEmpty();

        time.SetUtcNow(new DateTimeOffset(2026, 12, 15, 9, 0, 0, TimeSpan.Zero));
        await dispatcher.DispatchAsync();

        publisher.Sent.Should().BeEquivalentTo(
            [(users[0].Id, NotificationType.YearlyRecapReady), (users[1].Id, NotificationType.YearlyRecapReady)]);
    }

    [Fact]
    public void LongestWeeklyStreak_BreaksOnMissingWeek()
    {
        RecapCalculator.Played At(int month, int day) =>
            new(Guid.NewGuid(), Guid.NewGuid(), 60, null, new DateTimeOffset(2026, month, day, 15, 0, 0, TimeSpan.Zero), false);

        RecapCalculator.LongestWeeklyStreak([At(1, 5), At(1, 12), At(1, 26), At(2, 2), At(2, 9), At(2, 10)])
            .Should().Be(3);
    }

    private static (List<User> Users, Sport Sport) Seed(IApplicationDbContext db, int count)
    {
        var users = Enumerable.Range(0, count)
            .Select(index =>
            {
                var user = TestUsers.CreateActive($"+90555000000{index}", Now);
                user.AttachUserProfile(UserProfile.Create(user.Id, $"player{index}", $"Player {index}", Now));
                return user;
            })
            .ToList();
        var sport = Sport.Create("Football", 1, Now, "football");
        db.Users.AddRange(users);
        db.Sports.Add(sport);
        return (users, sport);
    }

    private static void AddPlayedEvent(
        IApplicationDbContext db,
        Guid organizerId,
        Guid playerId,
        Guid sportId,
        DateTimeOffset eventDate)
    {
        var createdAt = eventDate.AddDays(-3);
        var @event = DomainEvent.Create(
            organizerId, sportId, "Match", eventDate, 60,
            latitude: 41m, longitude: 29m, address: Maltepe,
            createdAt, maxParticipants: 8);
        @event.Publish(createdAt);
        var (participant, _) = @event.Apply(playerId, createdAt.AddHours(1));
        if (participant!.Status == ParticipantStatus.Pending)
        {
            @event.ApproveParticipant(playerId, createdAt.AddHours(2));
        }

        var afterEnd = eventDate.AddHours(3);
        @event.CompleteIfDue(afterEnd);
        @event.ConfirmAttendance(playerId, afterEnd);
        db.Events.Add(@event);
    }

    private sealed class RecordingPublisher : INotificationPublisher
    {
        public List<(Guid UserId, NotificationType Type)> Sent { get; } = [];

        public Task PublishAsync(
            Guid recipientUserId,
            NotificationType type,
            string title,
            string body,
            NotificationEntityType entityType,
            Guid? entityId,
            Guid? actorUserId,
            CancellationToken cancellationToken = default)
        {
            Sent.Add((recipientUserId, type));
            return Task.CompletedTask;
        }
    }
}
