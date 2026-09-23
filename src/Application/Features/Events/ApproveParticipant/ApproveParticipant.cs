using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Notifications;
using Sportner.Domain.Common.Enums;
using Sportner.Localization.Resources;

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

                // Re-check age at approval time, not just at application time: birth
                // date can no longer change after it's set, but this still guards
                // against approving eligibility drift from stale/edge-case data.
                var participantProfile = await DbContext.UserProfiles.AsNoTracking()
                    .Where(profile => profile.UserId == request.UserId)
                    .Select(profile => new { profile.BirthDate, profile.Gender })
                    .FirstOrDefaultAsync(ct);

                if (participantProfile?.BirthDate is null)
                {
                    return Result.Failure(EventErrors.ParticipantBirthDateMissing);
                }

                if (!@event.IsParticipantAgeEligible(participantProfile.BirthDate.Value))
                {
                    return Result.Failure(EventErrors.ParticipantAgeNotEligible);
                }

                if (!@event.IsParticipantGenderEligible(participantProfile.Gender))
                {
                    return Result.Failure(EventErrors.ParticipantGenderNotEligible);
                }

                if (!@event.HasAvailableCapacity())
                {
                    return Result.Failure(EventErrors.CapacityFull);
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

                var recipientLanguage = await NotificationActor.ResolveRecipientLanguageAsync(
                    DbContext, request.UserId, ct);

                await _notificationPublisher.PublishAsync(
                    request.UserId,
                    NotificationType.EventRequestApproved,
                    NotificationActor.Format(
                        recipientLanguage,
                        nameof(NotificationsResource.EventRequestApproved_Title),
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
