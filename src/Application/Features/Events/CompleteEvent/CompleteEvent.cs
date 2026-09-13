using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Gamification;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Events;
using Sportner.Application.Features.Quests;

namespace Sportner.Application.Features.Events.CompleteEvent;

public sealed record CompleteEventCommand(Guid EventId) : ICommand<EventResponse>;

internal sealed class CompleteEventCommandHandler
    : OrganizerEventMutationHandlerBase, ICommandHandler<CompleteEventCommand, EventResponse>
{
    private readonly IBadgeAwarder _badgeAwarder;
    private readonly IQuestProgressTracker _questProgressTracker;
    private readonly INotificationPublisher _notificationPublisher;

    public CompleteEventCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        IBadgeAwarder badgeAwarder,
        IQuestProgressTracker questProgressTracker,
        INotificationPublisher notificationPublisher)
        : base(dbContext, currentUser, timeProvider)
    {
        _badgeAwarder = badgeAwarder;
        _questProgressTracker = questProgressTracker;
        _notificationPublisher = notificationPublisher;
    }

    public Task<Result<EventResponse>> Handle(
        CompleteEventCommand request,
        CancellationToken cancellationToken) =>
        MutateAsync(
            request.EventId,
            async (@event, utcNow, ct) =>
            {
                @event.Complete(utcNow);
                await EventCompletion.ApplySideEffectsAsync(
                    DbContext,
                    @event,
                    _badgeAwarder,
                    _questProgressTracker,
                    _notificationPublisher,
                    utcNow,
                    ct);
                return Result.Success();
            },
            cancellationToken);
}
