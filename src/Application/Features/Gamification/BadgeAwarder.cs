using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sportner.Application.Abstractions.Gamification;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Domain.Badges;
using Sportner.Domain.Common.Constants;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Gamification;

internal sealed class BadgeAwarder : IBadgeAwarder
{
    private readonly IApplicationDbContext _dbContext;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<BadgeAwarder> _logger;

    public BadgeAwarder(
        IApplicationDbContext dbContext,
        INotificationPublisher notificationPublisher,
        TimeProvider timeProvider,
        ILogger<BadgeAwarder> logger)
    {
        _dbContext = dbContext;
        _notificationPublisher = notificationPublisher;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task TryAwardAsync(
        Guid userId,
        string badgeCode,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(badgeCode))
        {
            return;
        }

        var badge = await _dbContext.Badges
            .FirstOrDefaultAsync(
                candidate => candidate.Code == badgeCode && candidate.IsActive,
                cancellationToken);

        if (badge is null || !badge.IsEarnable())
        {
            return;
        }

        var alreadyAwarded = await _dbContext.UserBadges.AsNoTracking()
            .AnyAsync(
                userBadge => userBadge.UserId == userId && userBadge.BadgeId == badge.Id,
                cancellationToken);

        if (alreadyAwarded)
        {
            return;
        }

        var utcNow = _timeProvider.GetUtcNow();
        _dbContext.UserBadges.Add(UserBadge.Award(userId, badge.Id, utcNow, utcNow));

        var statistics = await _dbContext.UserStatistics
            .FirstOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken);

        statistics?.IncreaseBadgesCount(utcNow);

        await _notificationPublisher.PublishAsync(
            userId,
            NotificationType.BadgeEarned,
            "Rozet kazandın",
            $"'{badge.Name}' rozetini kazandın.",
            NotificationEntityType.Badge,
            badge.Id,
            actorUserId: null,
            cancellationToken);
    }

    public async Task EvaluateAfterAttendanceAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await EvaluateSportsExplorerAsync(userId, cancellationToken);
        await EvaluateEventMasterAsync(userId, cancellationToken);
        await EvaluateMarathonRunnerAsync(userId, cancellationToken);
    }

    public Task EvaluateAfterUserSportChangedAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        EvaluateSportsExplorerAsync(userId, cancellationToken);

    public Task EvaluateAfterCommentCreatedAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        EvaluateCommunityHelperAsync(userId, cancellationToken);

    public Task EvaluateAfterReportResolvedAsync(
        Guid reporterUserId,
        CancellationToken cancellationToken = default) =>
        EvaluateCommunityHelperAsync(reporterUserId, cancellationToken);

    public async Task SweepMarathonRunnersAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = _timeProvider.GetUtcNow();
        var lookback = utcNow.AddDays(-7);

        var candidateUserIds = await (
            from participant in _dbContext.EventParticipants.AsNoTracking()
            join @event in _dbContext.Events.AsNoTracking()
                on participant.EventId equals @event.Id
            where participant.Status == ParticipantStatus.Attended
                  && @event.EventDate >= lookback
            select participant.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Marathon runner sweep evaluating {Count} recent attendees.",
            candidateUserIds.Count);

        foreach (var userId in candidateUserIds)
        {
            await EvaluateMarathonRunnerAsync(userId, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EvaluateSportsExplorerAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var profileSports = await _dbContext.UserSports.AsNoTracking()
            .CountAsync(userSport => userSport.UserId == userId, cancellationToken);

        if (profileSports >= BadgeThresholds.SportsExplorerDistinctSports)
        {
            await TryAwardAsync(userId, BadgeCodes.SportsExplorer, cancellationToken);
            return;
        }

        var attendedSports = await (
            from participant in _dbContext.EventParticipants.AsNoTracking()
            join @event in _dbContext.Events.AsNoTracking()
                on participant.EventId equals @event.Id
            where participant.UserId == userId
                  && participant.Status == ParticipantStatus.Attended
            select @event.SportId)
            .Distinct()
            .CountAsync(cancellationToken);

        if (attendedSports >= BadgeThresholds.SportsExplorerDistinctSports)
        {
            await TryAwardAsync(userId, BadgeCodes.SportsExplorer, cancellationToken);
        }
    }

    private async Task EvaluateEventMasterAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var attended = await _dbContext.EventParticipants.AsNoTracking()
            .CountAsync(
                participant =>
                    participant.UserId == userId
                    && participant.Status == ParticipantStatus.Attended,
                cancellationToken);

        if (attended >= BadgeThresholds.EventMasterAttended)
        {
            await TryAwardAsync(userId, BadgeCodes.EventMaster, cancellationToken);
        }
    }

    private async Task EvaluateCommunityHelperAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var resolvedReports = await _dbContext.Reports.AsNoTracking()
            .CountAsync(
                report =>
                    report.ReporterUserId == userId
                    && report.Status == ReportStatus.Resolved,
                cancellationToken);

        if (resolvedReports >= BadgeThresholds.CommunityHelperResolvedReports)
        {
            await TryAwardAsync(userId, BadgeCodes.CommunityHelper, cancellationToken);
            return;
        }

        var comments = await _dbContext.PostComments.AsNoTracking()
            .CountAsync(comment => comment.UserId == userId, cancellationToken);

        if (comments >= BadgeThresholds.CommunityHelperComments)
        {
            await TryAwardAsync(userId, BadgeCodes.CommunityHelper, cancellationToken);
        }
    }

    private async Task EvaluateMarathonRunnerAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var eventDates = await (
            from participant in _dbContext.EventParticipants.AsNoTracking()
            join @event in _dbContext.Events.AsNoTracking()
                on participant.EventId equals @event.Id
            where participant.UserId == userId
                  && participant.Status == ParticipantStatus.Attended
            select @event.EventDate)
            .ToListAsync(cancellationToken);

        if (!HasConsecutiveWeekStreak(eventDates, BadgeThresholds.MarathonConsecutiveWeeks))
        {
            return;
        }

        await TryAwardAsync(userId, BadgeCodes.MarathonRunner, cancellationToken);
    }

    /// <summary>
    /// ISO week (year, week) uniqueness; requires <paramref name="requiredWeeks"/> consecutive weeks.
    /// </summary>
    internal static bool HasConsecutiveWeekStreak(
        IEnumerable<DateTimeOffset> eventDates,
        int requiredWeeks)
    {
        var weeks = eventDates
            .Select(ToIsoYearWeek)
            .Distinct()
            .OrderBy(week => week.Year)
            .ThenBy(week => week.Week)
            .ToList();

        if (weeks.Count < requiredWeeks)
        {
            return false;
        }

        var streak = 1;
        for (var i = 1; i < weeks.Count; i++)
        {
            if (IsNextIsoWeek(weeks[i - 1], weeks[i]))
            {
                streak++;
                if (streak >= requiredWeeks)
                {
                    return true;
                }
            }
            else
            {
                streak = 1;
            }
        }

        return false;
    }

    private static (int Year, int Week) ToIsoYearWeek(DateTimeOffset value)
    {
        var date = DateOnly.FromDateTime(value.UtcDateTime);
        return (ISOWeek.GetYear(date.ToDateTime(TimeOnly.MinValue)), ISOWeek.GetWeekOfYear(date.ToDateTime(TimeOnly.MinValue)));
    }

    private static bool IsNextIsoWeek((int Year, int Week) previous, (int Year, int Week) current)
    {
        if (previous.Year == current.Year)
        {
            return current.Week == previous.Week + 1;
        }

        if (current.Year == previous.Year + 1 && current.Week == 1)
        {
            var lastWeek = ISOWeek.GetWeekOfYear(new DateTime(previous.Year, 12, 28));
            return previous.Week == lastWeek;
        }

        return false;
    }
}
