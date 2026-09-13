using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Gamification;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Features.Quests;
using Sportner.Domain.Common.Constants;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Events;

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
        CancellationToken cancellationToken)
    {
        var participant = @event.Participants.FirstOrDefault(candidate => candidate.UserId == userId);
        var shouldCreditAttendance = participant?.Status is ParticipantStatus.Approved;

        @event.ConfirmAttendance(userId, utcNow);

        if (!shouldCreditAttendance)
        {
            return;
        }

        var statistics = await dbContext.UserStatistics
            .FirstOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken);

        statistics?.IncreaseCompletedEvents(utcNow);
        await RefreshAttendanceRateAsync(dbContext, userId, utcNow, cancellationToken);

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
        CancellationToken cancellationToken)
    {
        @event.MarkNoShow(userId, utcNow);

        var statistics = await dbContext.UserStatistics
            .FirstOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken);

        if (statistics is not null)
        {
            await RefreshAttendanceRateAsync(dbContext, userId, utcNow, cancellationToken);
        }
    }

    private static async Task RefreshAttendanceRateAsync(
        IApplicationDbContext dbContext,
        Guid userId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var statistics = await dbContext.UserStatistics
            .FirstOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken);

        if (statistics is null || statistics.EventsJoined == 0)
        {
            return;
        }

        // Includes the just-marked transition, which is tracked in the aggregate but may not
        // yet be visible to a fresh AsNoTracking query against the same DbContext.
        var attended = await dbContext.EventParticipants
            .CountAsync(
                participant =>
                    participant.UserId == userId
                    && participant.Status == ParticipantStatus.Attended,
                cancellationToken);

        var noShow = await dbContext.EventParticipants
            .CountAsync(
                participant =>
                    participant.UserId == userId
                    && participant.Status == ParticipantStatus.NoShow,
                cancellationToken);

        var decided = attended + noShow;

        if (decided == 0)
        {
            return;
        }

        statistics.UpdateAttendanceRate(attended * 100m / decided, utcNow);
    }
}
