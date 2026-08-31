using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Notifications;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Events.RejectParticipant;

public sealed record RejectParticipantCommand(Guid EventId, Guid UserId) : ICommand<EventResponse>;

internal sealed class RejectParticipantCommandHandler
    : OrganizerEventMutationHandlerBase, ICommandHandler<RejectParticipantCommand, EventResponse>
{
    private readonly INotificationPublisher _notificationPublisher;

    public RejectParticipantCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        INotificationPublisher notificationPublisher)
        : base(dbContext, currentUser, timeProvider)
    {
        _notificationPublisher = notificationPublisher;
    }

    public Task<Result<EventResponse>> Handle(
        RejectParticipantCommand request,
        CancellationToken cancellationToken) =>
        MutateAsync(
            request.EventId,
            async (@event, utcNow, ct) =>
            {
                if (@event.Participants.All(participant => participant.UserId != request.UserId))
                {
                    return Result.Failure(EventErrors.ParticipantNotFound);
                }

                @event.RejectParticipant(request.UserId, utcNow);

                await _notificationPublisher.PublishAsync(
                    request.UserId,
                    NotificationType.EventRequestRejected,
                    await NotificationActor.TitleAsync(
                        DbContext,
                        @event.OrganizerUserId,
                        "başvurunu reddetti",
                        ct),
                    $"\"{@event.Title}\" etkinliğine başvurun reddedildi.",
                    NotificationEntityType.Event,
                    @event.Id,
                    @event.OrganizerUserId,
                    ct);

                return Result.Success();
            },
            cancellationToken);
}
