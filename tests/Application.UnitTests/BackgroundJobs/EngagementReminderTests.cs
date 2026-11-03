using FluentAssertions;
using Sportner.Application.BackgroundJobs;
using Sportner.Application.Features.Identity.UserProfiles.Streaks;
using Sportner.Domain.Common.Enums;
using static Sportner.Application.BackgroundJobs.EngagementReminderDispatcher;

namespace Sportner.Application.UnitTests.BackgroundJobs;

public sealed class EngagementReminderTests
{
    // Perşembe, 5 Kasım 2026 18:00 İstanbul.
    private static readonly DateTimeOffset Thursday = new(2026, 11, 5, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Thursday_ActiveStreakWithoutGameThisWeek_WarnsAboutStreak()
    {
        var reminder = EngagementReminderDispatcher.Decide(Context(
            Thursday,
            StreakQueries.Calculate([Thursday.AddDays(-7), Thursday.AddDays(-14)], 1, Thursday),
            newEvents: [NewEvent()]));

        reminder!.Type.Should().Be(NotificationType.StreakAtRisk);
        reminder.Count.Should().Be(2);
    }

    [Fact]
    public void Saturday_GoalHalfDone_NudgesGoal()
    {
        var saturday = Thursday.AddDays(2);
        var reminder = EngagementReminderDispatcher.Decide(Context(
            saturday,
            StreakQueries.Calculate([saturday.AddDays(-1)], 3, saturday)));

        reminder!.Type.Should().Be(NotificationType.WeeklyGoalNudge);
        reminder.Count.Should().Be(2);
    }

    [Fact]
    public void NewEventsInFavoriteSports_AreAnnounced_SingleOneLinksToEvent()
    {
        var newEvent = NewEvent();
        var reminder = EngagementReminderDispatcher.Decide(Context(
            Thursday.AddDays(-1),
            StreakQueries.Calculate([], 1, Thursday),
            newEvents: [newEvent]));

        reminder!.Type.Should().Be(NotificationType.NewEventsNearby);
        reminder.EventId.Should().Be(newEvent.Id);
    }

    [Fact]
    public void InactiveUser_GetsComeBack_OnlyWithEventsAndOutsideCooldown()
    {
        var streak = StreakQueries.Calculate([], 1, Thursday);
        var away = Thursday.AddDays(-20);

        EngagementReminderDispatcher.Decide(Context(Thursday, streak, cityEvents: 3, lastSeen: away))!
            .Type.Should().Be(NotificationType.ComeBack);
        EngagementReminderDispatcher.Decide(Context(Thursday, streak, cityEvents: 0, lastSeen: away))
            .Should().BeNull();
        EngagementReminderDispatcher.Decide(
                Context(Thursday, streak, cityEvents: 3, lastSeen: away, comeBackRecently: true))
            .Should().BeNull();
        EngagementReminderDispatcher.Decide(
                Context(Thursday, streak, cityEvents: 3, lastSeen: Thursday.AddDays(-2)))
            .Should().BeNull();
    }

    private static ReminderContext Context(
        DateTimeOffset utcNow,
        StreakResponse streak,
        int cityEvents = 0,
        IReadOnlyList<UpcomingEvent>? newEvents = null,
        DateTimeOffset? lastSeen = null,
        bool comeBackRecently = false) =>
        new(
            utcNow,
            utcNow.ToOffset(StreakQueries.IstanbulOffset).DayOfWeek,
            streak,
            cityEvents,
            newEvents ?? [],
            lastSeen,
            comeBackRecently);

    private static UpcomingEvent NewEvent() =>
        new(Guid.NewGuid(), "Halı saha", Guid.NewGuid(), "Kadıköy/İstanbul", Guid.NewGuid(),
            Thursday.AddHours(-3), Thursday.AddDays(2), IsOpen: true);
}
