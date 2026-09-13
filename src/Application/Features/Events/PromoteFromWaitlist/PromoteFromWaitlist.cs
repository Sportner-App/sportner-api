using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Notifications;
using Sportner.Domain.Common.Enums;
using Sportner.Localization.Resources;

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

                var existing = @event.Participants.FirstOrDefault(
                    participant => participant.UserId == request.UserId);
                var promoted = @event.PromoteFromWaitlist(request.UserId, utcNow);

                if (existing is null)
                {
                    DbContext.MarkAsAdded(promoted);
                }

                await EventAccess.AddConversationMemberIfPresentAsync(
                    DbContext,
                    @event.Id,
                    request.UserId,
                    utcNow,
                    ct);

                var statistics = await DbContext.UserStatistics
                    .FirstOrDefaultAsync(candidate => candidate.UserId == request.UserId, ct);

                statistics?.IncreaseEventsJoined(utcNow);

                var recipientLanguage = await NotificationActor.ResolveRecipientLanguageAsync(
                    DbContext, request.UserId, ct);

                await _notificationPublisher.PublishAsync(
                    request.UserId,
                    NotificationType.EventRequestApproved,
                    NotificationActor.Format(
                        recipientLanguage,
                        nameof(NotificationsResource.EventPromotedFromWaitlist_Title),
                        await NotificationActor.PrefixAsync(
                            DbContext, @event.OrganizerUserId, recipientLanguage, ct)),
                    NotificationActor.Format(
                        recipientLanguage,
                        nameof(NotificationsResource.EventRequestApproved_Body),
                        @event.Title),
                    NotificationEntityType.Event,
                    @event.Id,
                    @event.OrganizerUserId,
                    ct);

                return Result.Success();
            },
            cancellationToken);
}
