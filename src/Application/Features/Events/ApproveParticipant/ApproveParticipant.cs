using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Events.ApproveParticipant;

public sealed record ApproveParticipantCommand(Guid EventId, Guid UserId) : ICommand<EventResponse>;

internal sealed class ApproveParticipantCommandHandler
    : OrganizerEventMutationHandlerBase, ICommandHandler<ApproveParticipantCommand, EventResponse>
{
    private readonly INotificationPublisher _notificationPublisher;

    public ApproveParticipantCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        INotificationPublisher notificationPublisher)
        : base(dbContext, currentUser, timeProvider)
    {
        _notificationPublisher = notificationPublisher;
    }

    public Task<Result<EventResponse>> Handle(
        ApproveParticipantCommand request,
        CancellationToken cancellationToken) =>
        MutateAsync(
            request.EventId,
            async (@event, utcNow, ct) =>
            {
                if (@event.Participants.All(participant => participant.UserId != request.UserId))
                {
                    return Result.Failure(EventErrors.ParticipantNotFound);
                }

                @event.ApproveParticipant(request.UserId, utcNow);

                await EventAccess.AddConversationMemberIfPresentAsync(
                    DbContext,
                    @event.Id,
                    request.UserId,
                    utcNow,
                    ct);

                var statistics = await DbContext.UserStatistics
                    .FirstOrDefaultAsync(candidate => candidate.UserId == request.UserId, ct);

                statistics?.IncreaseEventsJoined(utcNow);

                await _notificationPublisher.PublishAsync(
                    request.UserId,
                    NotificationType.EventRequestApproved,
                    "Başvurun onaylandı",
                    $"\"{@event.Title}\" etkinliğine katılımın onaylandı.",
                    NotificationEntityType.Event,
                    @event.Id,
                    @event.OrganizerUserId,
                    ct);

                return Result.Success();
            },
            cancellationToken);
}
