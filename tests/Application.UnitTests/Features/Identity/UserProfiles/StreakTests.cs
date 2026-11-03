using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Sportner.Application.Features.Identity.UserProfiles.Streaks;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Sports;
using Sportner.Domain.Users;
using DomainEvent = Sportner.Domain.Events.Event;

namespace Sportner.Application.UnitTests.Features.Identity.UserProfiles;

public sealed class StreakTests
{
    // Çarşamba, 4 Kasım 2026 12:00 İstanbul.
    private static readonly DateTimeOffset Now = new(2026, 11, 4, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void EmptyCurrentWeek_KeepsStreakFromPreviousWeeks()
    {
        var result = StreakQueries.Calculate(
            [Now.AddDays(-7), Now.AddDays(-14), Now.AddDays(-21)],
            weeklyGoal: 2,
            Now);

        result.CurrentStreak.Should().Be(3);
        result.ThisWeekCount.Should().Be(0);
        result.WeeklyGoal.Should().Be(2);
        result.RecentWeeks.Should().HaveCount(StreakQueries.RecentWeeksCount);
        result.RecentWeeks[^1].WeekStart.Should().Be(new DateOnly(2026, 11, 2));
    }

    [Fact]
    public void SkippedWeek_BreaksStreak_ButLongestIsKept()
    {
        var result = StreakQueries.Calculate(
            [Now, Now.AddDays(-14), Now.AddDays(-21), Now.AddDays(-28), Now.AddDays(-28)],
            weeklyGoal: 1,
            Now);

        result.CurrentStreak.Should().Be(1);
        result.LongestStreak.Should().Be(3);
        result.ThisWeekCount.Should().Be(1);
    }

    [Fact]
    public void WeeksFollowIstanbulCalendar()
    {
        // Pazar 23:30 İstanbul = Pazar 20:30 UTC: önceki haftaya; Pazartesi 00:30 İstanbul bu haftaya.
        var sundayNight = new DateTimeOffset(2026, 11, 1, 20, 30, 0, TimeSpan.Zero);
        var mondayEarly = new DateTimeOffset(2026, 11, 1, 21, 30, 0, TimeSpan.Zero);

        var result = StreakQueries.Calculate([sundayNight, mondayEarly], weeklyGoal: 1, Now);

        result.ThisWeekCount.Should().Be(1);
        result.CurrentStreak.Should().Be(2);
    }

    [Fact]
    public async Task OrganizedAndAttendedEventsCountOnce()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(Now.AddDays(-1));
        var organizer = TestUsers.CreateActive("+905551111111", time.GetUtcNow());
        organizer.AttachUserProfile(UserProfile.Create(organizer.Id, "org", "Org", time.GetUtcNow()));
        var sport = Sport.Create("Football", 1, time.GetUtcNow(), "football");
        var @event = DomainEvent.Create(
            organizer.Id,
            sport.Id,
            "Match",
            time.GetUtcNow().AddHours(2),
            60,
            latitude: 41m,
            longitude: 29m,
            address: "Istanbul",
            time.GetUtcNow(),
            maxParticipants: 8);
        @event.Publish(time.GetUtcNow());
        @event.CompleteIfDue(Now);
        @event.ConfirmAttendance(organizer.Id, Now);
        db.Users.Add(organizer);
        db.Sports.Add(sport);
        db.Events.Add(@event);
        await db.SaveChangesAsync();

        var result = await new GetStreakQueryHandler(
                db, new TestCurrentUser(organizer.Id), new FakeTimeProvider(Now))
            .Handle(new GetStreakQuery(organizer.Id), CancellationToken.None);

        result.Value!.ThisWeekCount.Should().Be(1);
        result.Value.CurrentStreak.Should().Be(1);
    }
}
