using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Gamification;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Features.Quests;
using Sportner.Domain.Common.Constants;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Events;
using Sportner.Domain.Users;

namespace Sportner.Application.Features.Events;

/// <summary>
/// Shared per-participant attendance side effects, used by the single-participant commands
/// (ConfirmAttendance, MarkNoShow), the organizer's bulk "confirm all" action, and the
/// auto-confirm safety-net background job — so the stats/badge/quest/notification side effects
/// stay in one place regardless of which of those triggers the transition.
/// </summary>
internal static class AttendanceConfirmation
{
    internal static async Task ConfirmAsync(
        IApplicationDbContext dbContext,
        Event @event,
        Guid userId,
        IBadgeAwarder badgeAwarder,
        IQuestProgressTracker questProgressTracker,
        INotificationPublisher notificationPublisher,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<Guid, UserStatistics>? preloadedStatistics = null)
    {
        var participant = @event.Participants.FirstOrDefault(candidate => candidate.UserId == userId);
        var shouldCreditAttendance = participant?.Status is ParticipantStatus.Approved;

        @event.ConfirmAttendance(userId, utcNow);

        if (!shouldCreditAttendance)
        {
            return;
        }

        var statistics = preloadedStatistics is not null
            ? preloadedStatistics.GetValueOrDefault(userId)
            : await dbContext.UserStatistics
                .FirstOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken);

        statistics?.IncreaseCompletedEvents(utcNow);
        await RefreshAttendanceRateAsync(dbContext, statistics, userId, utcNow, cancellationToken);

        await badgeAwarder.TryAwardAsync(userId, BadgeCodes.FirstEvent, cancellationToken);
        await badgeAwarder.EvaluateAfterAttendanceAsync(userId, cancellationToken);

        await questProgressTracker.ReportAsync(
            userId,
            QuestMetrics.EventsAttended,
            1,
            cancellationToken);

        // Only now — Approved → Attended — is this participant eligible to review (and be
        // reviewed by) the rest of the event, per ReviewEligibility.
        await EventCompletion.SendReviewPromptAsync(
            dbContext, @event, userId, notificationPublisher, cancellationToken);
    }

    internal static async Task MarkAbsentAsync(
        IApplicationDbContext dbContext,
        Event @event,
        Guid userId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<Guid, UserStatistics>? preloadedStatistics = null)
    {
        @event.MarkNoShow(userId, utcNow);

        var statistics = preloadedStatistics is not null
            ? preloadedStatistics.GetValueOrDefault(userId)
            : await dbContext.UserStatistics
                .FirstOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken);

        await RefreshAttendanceRateAsync(dbContext, statistics, userId, utcNow, cancellationToken);
    }

    /// <summary>Recomputes the attendance rate from an already-loaded statistics row (no re-fetch).</summary>
    private static async Task RefreshAttendanceRateAsync(
        IApplicationDbContext dbContext,
        UserStatistics? statistics,
        Guid userId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (statistics is null || statistics.EventsJoined == 0)
        {
            return;
        }

        // Includes the just-marked transition, which is tracked in the aggregate but may not
        // yet be visible to a fresh AsNoTracking query against the same DbContext. One grouped
        // query instead of two separate counts.
        var counts = await dbContext.EventParticipants.AsNoTracking()
            .Where(participant =>
                participant.UserId == userId
                && (participant.Status == ParticipantStatus.Attended
                    || participant.Status == ParticipantStatus.NoShow))
            .GroupBy(participant => participant.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var attended = counts.FirstOrDefault(c => c.Status == ParticipantStatus.Attended)?.Count ?? 0;
        var noShow = counts.FirstOrDefault(c => c.Status == ParticipantStatus.NoShow)?.Count ?? 0;
        var decided = attended + noShow;

        if (decided == 0)
        {
            return;
        }

        statistics.UpdateAttendanceRate(attended * 100m / decided, utcNow);
    }
}
