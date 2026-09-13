using Sportner.Application.Abstractions.Gamification;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Localization;
using Sportner.Application.Features.Notifications;
using Sportner.Application.Features.Quests;
using Sportner.Domain.Common.Constants;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Events;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Events;

internal static class EventCompletion
{
    internal static async Task ApplySideEffectsAsync(
        IApplicationDbContext dbContext,
        Event @event,
        IBadgeAwarder badgeAwarder,
        IQuestProgressTracker questProgressTracker,
        INotificationPublisher notificationPublisher,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        await EventAccess.CloseEventConversationAsync(dbContext, @event.Id, utcNow, cancellationToken);
        await badgeAwarder.EvaluateAfterEventCompletedAsync(@event.OrganizerUserId, cancellationToken);
        await questProgressTracker.ReportAsync(
            @event.OrganizerUserId,
            QuestMetrics.EventsOrganizedCompleted,
            1,
            cancellationToken);

        // Participants only become review-eligible once their attendance is confirmed, which
        // domain rules require to happen AFTER the event completes (see ConfirmAttendance) — so
        // at this exact moment only the organizer is already eligible to review. Each
        // participant gets their own review-prompt notification when their attendance is
        // confirmed instead (ConfirmAttendanceCommandHandler).
        await SendReviewPromptAsync(
            dbContext, @event, @event.OrganizerUserId, notificationPublisher, cancellationToken);
    }

    /// <summary>Prompts <paramref name="recipientId"/> to go rate their teammates for this event.</summary>
    internal static async Task SendReviewPromptAsync(
        IApplicationDbContext dbContext,
        Event @event,
        Guid recipientId,
        INotificationPublisher notificationPublisher,
        CancellationToken cancellationToken)
    {
        var language = await NotificationActor.ResolveRecipientLanguageAsync(
            dbContext, recipientId, cancellationToken);

        await notificationPublisher.PublishAsync(
            recipientId,
            NotificationType.EventReviewPrompt,
            NotificationActor.Format(language, nameof(NotificationsResource.EventReviewPrompt_Title)),
            NotificationActor.Format(
                language, nameof(NotificationsResource.EventReviewPrompt_Body), @event.Title),
            NotificationEntityType.Event,
            @event.Id,
            actorUserId: null,
            cancellationToken);
    }
}
