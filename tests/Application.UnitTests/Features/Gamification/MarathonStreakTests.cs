using FluentAssertions;
using Sportner.Application.Features.Gamification;
using Sportner.Domain.Common.Constants;

namespace Sportner.Application.UnitTests.Features.Gamification;

public sealed class MarathonStreakTests
{
    [Fact]
    public void HasConsecutiveWeekStreak_FourWeeksInARow_ReturnsTrue()
    {
        var start = new DateTimeOffset(2026, 1, 5, 12, 0, 0, TimeSpan.Zero); // Monday ISO week
        var dates = Enumerable.Range(0, 4)
            .Select(week => start.AddDays(week * 7))
            .ToArray();

        BadgeAwarder.HasConsecutiveWeekStreak(dates, BadgeThresholds.MarathonConsecutiveWeeks)
            .Should().BeTrue();
    }

    [Fact]
    public void HasConsecutiveWeekStreak_GapInWeeks_ReturnsFalse()
    {
        var start = new DateTimeOffset(2026, 1, 5, 12, 0, 0, TimeSpan.Zero);
        DateTimeOffset[] dates =
        [
            start,
            start.AddDays(7),
            start.AddDays(21), // skip week 3
            start.AddDays(28)
        ];

        BadgeAwarder.HasConsecutiveWeekStreak(dates, BadgeThresholds.MarathonConsecutiveWeeks)
            .Should().BeFalse();
    }

    [Fact]
    public void HasConsecutiveWeekStreak_BelowThreshold_ReturnsFalse()
    {
        var start = new DateTimeOffset(2026, 1, 5, 12, 0, 0, TimeSpan.Zero);
        var dates = Enumerable.Range(0, 3)
            .Select(week => start.AddDays(week * 7))
            .ToArray();

        BadgeAwarder.HasConsecutiveWeekStreak(dates, BadgeThresholds.MarathonConsecutiveWeeks)
            .Should().BeFalse();
    }
}
