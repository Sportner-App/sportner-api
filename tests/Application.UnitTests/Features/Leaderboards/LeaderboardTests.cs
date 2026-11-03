using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.BackgroundJobs;
using Sportner.Application.Features.Leaderboards;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Sports;
using Sportner.Domain.Users;
using DomainEvent = Sportner.Domain.Events.Event;

namespace Sportner.Application.UnitTests.Features.Leaderboards;

public sealed class LeaderboardTests
{
    // 10 Kasım 2026 12:00 İstanbul.
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);
    private const string Maltepe = "Cd. No:1, 34841 Maltepe/İstanbul, Türkiye";

    [Fact]
    public void Rank_BreaksTiesByWhoReachedTheCountFirst()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        LeaderboardCalculator.Row Row(Guid user, int daysAgo) =>
            new(user, "İstanbul", "Maltepe", Guid.NewGuid(), Now.AddDays(-daysAgo));

        var standings = LeaderboardCalculator.Rank(
            [Row(a, 5), Row(a, 1), Row(b, 4), Row(b, 3), Row(c, 2)]);

        standings.Select(standing => standing.UserId).Should().Equal(b, a, c);
        standings.Select(standing => standing.Rank).Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task Leaderboard_HidesPrivateAndOptedOut_AndRequiresMinimumPlayers()
    {
        await using var db = InMemoryDb.Create();
        var (users, sport) = Seed(db, 4);
        users[2].UserProfile!.UpdateVisibility(false, Now);
        users[3].UserProfile!.UpdateLeaderboardVisibility(false, Now);
        AddPlayedEvent(db, users[0].Id, sport.Id, Maltepe);
        AddPlayedEvent(db, users[1].Id, sport.Id, Maltepe);
        AddPlayedEvent(db, users[2].Id, sport.Id, Maltepe);
        AddPlayedEvent(db, users[3].Id, sport.Id, Maltepe);
        await db.SaveChangesAsync();

        var result = await new GetDistrictLeaderboardQueryHandler(
                db, new TestCurrentUser(users[0].Id), new FakeTimeProvider(Now))
            .Handle(
                new GetDistrictLeaderboardQuery("İstanbul", "Maltepe", null, LeaderboardPeriod.Month),
                CancellationToken.None);

        result.Value!.TotalPlayers.Should().Be(2);
        result.Value.Entries.Should().BeEmpty();
        result.Value.Me!.Rank.Should().Be(1);
    }

    [Fact]
    public async Task Dispatcher_NotifiesNewLeaderOnce_AndCrownsLastMonth()
    {
        await using var db = InMemoryDb.Create();
        var (users, sport) = Seed(db, 3);
        AddPlayedEvent(db, users[0].Id, sport.Id, Maltepe);
        AddPlayedEvent(db, users[0].Id, sport.Id, Maltepe);
        AddPlayedEvent(db, users[1].Id, sport.Id, Maltepe);
        AddPlayedEvent(db, users[2].Id, sport.Id, Maltepe);
        await db.SaveChangesAsync();

        var publisher = new RecordingPublisher();
        var time = new FakeTimeProvider(Now);
        var dispatcher = new DistrictLeaderDispatcher(
            db, publisher, time, NullLogger<DistrictLeaderDispatcher>.Instance);

        await dispatcher.DispatchAsync();
        await dispatcher.DispatchAsync();
        publisher.Sent.Should().Equal((users[0].Id, NotificationType.DistrictLeaderTaken));

        time.SetUtcNow(new DateTimeOffset(2026, 12, 3, 9, 0, 0, TimeSpan.Zero));
        await dispatcher.DispatchAsync();
        await dispatcher.DispatchAsync();

        publisher.Sent.Should().ContainSingle(sent => sent.Type == NotificationType.DistrictCrowned)
            .Which.UserId.Should().Be(users[0].Id);
        db.DistrictLeaders.Single().CrownedAt.Should().NotBeNull();
    }

    private static (List<User> Users, Sport Sport) Seed(Sportner.Application.Abstractions.Persistence.IApplicationDbContext db, int count)
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
        Sportner.Application.Abstractions.Persistence.IApplicationDbContext db,
        Guid organizerId,
        Guid sportId,
        string address)
    {
        var @event = DomainEvent.Create(
            organizerId, sportId, "Match", Now.AddDays(-2), 60,
            latitude: 41m, longitude: 29m, address: address,
            Now.AddDays(-3), maxParticipants: 8);
        @event.Publish(Now.AddDays(-3));
        @event.CompleteIfDue(Now);
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
