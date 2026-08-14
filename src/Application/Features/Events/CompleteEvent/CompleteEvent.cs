using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Gamification;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Quests;
using Sportner.Domain.Common.Constants;

namespace Sportner.Application.Features.Events.CompleteEvent;

public sealed record CompleteEventCommand(Guid EventId) : ICommand<EventResponse>;

internal sealed class CompleteEventCommandHandler
    : OrganizerEventMutationHandlerBase, ICommandHandler<CompleteEventCommand, EventResponse>
{
    private readonly IBadgeAwarder _badgeAwarder;
    private readonly IQuestProgressTracker _questProgressTracker;

    public CompleteEventCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        IBadgeAwarder badgeAwarder,
        IQuestProgressTracker questProgressTracker)
        : base(dbContext, currentUser, timeProvider)
    {
        _badgeAwarder = badgeAwarder;
        _questProgressTracker = questProgressTracker;
    }

    public Task<Result<EventResponse>> Handle(
        CompleteEventCommand request,
        CancellationToken cancellationToken) =>
        MutateAsync(
            request.EventId,
            async (@event, utcNow, ct) =>
            {
                @event.Complete(utcNow);
                await EventAccess.CloseEventConversationAsync(DbContext, @event.Id, utcNow, ct);
                await _badgeAwarder.EvaluateAfterEventCompletedAsync(@event.OrganizerUserId, ct);
                await _questProgressTracker.ReportAsync(
                    @event.OrganizerUserId,
                    QuestMetrics.EventsOrganizedCompleted,
                    1,
                    ct);
                return Result.Success();
            },
            cancellationToken);
}
