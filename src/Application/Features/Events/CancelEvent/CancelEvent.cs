using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Events.CancelEvent;

public sealed record CancelEventCommand(Guid EventId) : ICommand<EventResponse>;

internal sealed class CancelEventCommandHandler
    : OrganizerEventMutationHandlerBase, ICommandHandler<CancelEventCommand, EventResponse>
{
    private readonly INotificationPublisher _notificationPublisher;

    public CancelEventCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        INotificationPublisher notificationPublisher)
        : base(dbContext, currentUser, timeProvider)
    {
        _notificationPublisher = notificationPublisher;
    }

    public Task<Result<EventResponse>> Handle(
        CancelEventCommand request,
        CancellationToken cancellationToken) =>
        MutateAsync(
            request.EventId,
            async (@event, utcNow, ct) =>
            {
                await EventCancellation.CancelAsync(
                    DbContext, _notificationPublisher, @event, CurrentUser.UserId!.Value, utcNow, ct);

                return Result.Success();
            },
            cancellationToken);
}
