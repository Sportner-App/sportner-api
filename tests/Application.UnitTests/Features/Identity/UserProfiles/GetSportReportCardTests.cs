using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Sportner.Application.Features.Identity.UserProfiles.GetSportReportCard;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Sports;
using Sportner.Domain.Users;
using DomainEvent = Sportner.Domain.Events.Event;

namespace Sportner.Application.UnitTests.Features.Identity.UserProfiles;

public sealed class GetSportReportCardTests
{
    [Fact]
    public async Task CountsOnlyAttendedEventsOfTheYear()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero));
        var player = TestUsers.CreateActive("+905551111111", time.GetUtcNow());
        player.AttachUserProfile(UserProfile.Create(player.Id, "player", "Player", time.GetUtcNow()));
        var football = Sport.Create("Football", 1, time.GetUtcNow(), "football");
        var tennis = Sport.Create("Tennis", 2, time.GetUtcNow(), "tennis");

        var attendedFootball = PlayedEvent(player.Id, football.Id, time, durationMinutes: 90, attend: true);
        var attendedTennis = PlayedEvent(player.Id, tennis.Id, time, durationMinutes: null, attend: true);
        // Bitti ama yoklamada "geldi" işaretlenmedi: oynanmış sayılmaz.
        var notConfirmed = PlayedEvent(player.Id, football.Id, time, durationMinutes: 60, attend: false);

        db.Users.Add(player);
        db.Sports.AddRange(football, tennis);
        db.Events.AddRange(attendedFootball, attendedTennis, notConfirmed);
        await db.SaveChangesAsync();

        var result = await new GetSportReportCardQueryHandler(
                db, new TestCurrentUser(player.Id), time)
            .Handle(new GetSportReportCardQuery(player.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var card = result.Value!;
        card.Year.Should().Be(2026);
        card.EventsPlayed.Should().Be(2);
        card.EventsOrganized.Should().Be(3);
        card.MinutesPlayed.Should().Be(90 + DomainEvent.DefaultDurationMinutes);
        card.SportsCount.Should().Be(2);
        card.TopSports.Select(sport => sport.Slug).Should().BeEquivalentTo("football", "tennis");

        var lastYear = await new GetSportReportCardQueryHandler(
                db, new TestCurrentUser(player.Id), time)
            .Handle(new GetSportReportCardQuery(player.Id, 2025), CancellationToken.None);

        lastYear.Value!.EventsPlayed.Should().Be(0);
    }

    [Fact]
    public async Task PrivateProfile_IsHiddenFromOthers()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero));
        var owner = TestUsers.CreateActive("+905551111111", time.GetUtcNow());
        var profile = UserProfile.Create(owner.Id, "owner", "Owner", time.GetUtcNow());
        profile.UpdateVisibility(false, time.GetUtcNow());
        owner.AttachUserProfile(profile);
        var viewer = TestUsers.CreateActive("+905552222222", time.GetUtcNow());
        db.Users.AddRange(owner, viewer);
        await db.SaveChangesAsync();

        var asViewer = await new GetSportReportCardQueryHandler(
                db, new TestCurrentUser(viewer.Id), time)
            .Handle(new GetSportReportCardQuery(owner.Id), CancellationToken.None);
        var asOwner = await new GetSportReportCardQueryHandler(
                db, new TestCurrentUser(owner.Id), time)
            .Handle(new GetSportReportCardQuery(owner.Id), CancellationToken.None);

        asViewer.IsFailure.Should().BeTrue();
        asOwner.IsSuccess.Should().BeTrue();
    }

    private static DomainEvent PlayedEvent(
        Guid organizerId,
        Guid sportId,
        FakeTimeProvider time,
        int? durationMinutes,
        bool attend)
    {
        var @event = DomainEvent.Create(
            organizerId,
            sportId,
            "Match",
            time.GetUtcNow().AddHours(2),
            durationMinutes,
            latitude: 41m,
            longitude: 29m,
            address: "Istanbul",
            time.GetUtcNow(),
            maxParticipants: 8);
        @event.Publish(time.GetUtcNow());
        var afterEnd = time.GetUtcNow().AddDays(1);
        @event.CompleteIfDue(afterEnd);
        if (attend)
        {
            @event.ConfirmAttendance(organizerId, afterEnd);
        }

        return @event;
    }
}
