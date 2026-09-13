using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Notifications;
using Sportner.Domain.Common.Enums;
using Sportner.Localization.Resources;

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

                var recipientLanguage = await NotificationActor.ResolveRecipientLanguageAsync(
                    DbContext, request.UserId, ct);

                await _notificationPublisher.PublishAsync(
                    request.UserId,
                    NotificationType.EventRequestRejected,
                    NotificationActor.Format(
                        recipientLanguage,
                        nameof(NotificationsResource.EventRequestRejected_Title),
                        await NotificationActor.PrefixAsync(
                            DbContext, @event.OrganizerUserId, recipientLanguage, ct)),
                    NotificationActor.Format(
                        recipientLanguage,
                        nameof(NotificationsResource.EventRequestRejected_Body),
                        @event.Title),
                    NotificationEntityType.Event,
                    @event.Id,
                    @event.OrganizerUserId,
                    ct);

                return Result.Success();
            },
            cancellationToken);
}
