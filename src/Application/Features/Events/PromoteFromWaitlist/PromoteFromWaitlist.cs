using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Events.PromoteFromWaitlist;

public sealed record PromoteFromWaitlistCommand(Guid EventId, Guid UserId) : ICommand<EventResponse>;

internal sealed class PromoteFromWaitlistCommandHandler
    : OrganizerEventMutationHandlerBase, ICommandHandler<PromoteFromWaitlistCommand, EventResponse>
{
    private readonly INotificationPublisher _notificationPublisher;

    public PromoteFromWaitlistCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        INotificationPublisher notificationPublisher)
        : base(dbContext, currentUser, timeProvider)
    {
        _notificationPublisher = notificationPublisher;
    }

    public Task<Result<EventResponse>> Handle(
        PromoteFromWaitlistCommand request,
        CancellationToken cancellationToken) =>
        MutateAsync(
            request.EventId,
            async (@event, utcNow, ct) =>
            {
                if (@event.Waitlist.All(entry => entry.UserId != request.UserId))
                {
                    return Result.Failure(EventErrors.WaitlistEntryNotFound);
                }

                if (!@event.HasAvailableCapacity())
                {
                    return Result.Failure(EventErrors.CapacityFull);
                }

                var promoted = @event.PromoteFromWaitlist(request.UserId, utcNow);
                DbContext.MarkAsAdded(promoted);

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
                    "Bekleme listesinden alındın",
                    $"\"{@event.Title}\" etkinliğine katılımın onaylandı.",
                    NotificationEntityType.Event,
                    @event.Id,
                    @event.OrganizerUserId,
                    ct);

                return Result.Success();
            },
            cancellationToken);
}
