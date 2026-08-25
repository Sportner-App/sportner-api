using Sportner.Application.Abstractions.Gamification;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Features.Quests;
using Sportner.Domain.Common.Constants;
using Sportner.Domain.Events;

namespace Sportner.Application.Features.Events;

internal static class EventCompletion
{
    internal static async Task ApplySideEffectsAsync(
        IApplicationDbContext dbContext,
        Event @event,
        IBadgeAwarder badgeAwarder,
        IQuestProgressTracker questProgressTracker,
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
    }
}
