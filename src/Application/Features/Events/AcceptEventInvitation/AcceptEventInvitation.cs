using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Notifications;
using Sportner.Domain.Common.Enums;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Events.AcceptEventInvitation;

public sealed record AcceptEventInvitationCommand(Guid EventId) : ICommand<EventResponse>;

internal sealed class AcceptEventInvitationCommandHandler
    : ICommandHandler<AcceptEventInvitationCommand, EventResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly INotificationPublisher _notificationPublisher;

    public AcceptEventInvitationCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        INotificationPublisher notificationPublisher)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _notificationPublisher = notificationPublisher;
    }

    public async Task<Result<EventResponse>> Handle(
        AcceptEventInvitationCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<EventResponse>.Failure(EventErrors.NotAuthenticated);
        }

        var @event = await EventAccess.LoadAggregateAsync(_dbContext, request.EventId, cancellationToken);
        if (@event is null)
        {
            return Result<EventResponse>.Failure(EventErrors.NotFound);
        }

        var participant = @event.Participants.FirstOrDefault(item => item.UserId == userId);
        if (participant?.Status is not Domain.Common.Enums.ParticipantStatus.Invited)
        {
            return Result<EventResponse>.Failure(EventErrors.InvitationNotFound);
        }

        if (!@event.HasAvailableCapacity())
        {
            return Result<EventResponse>.Failure(EventErrors.CapacityFull);
        }

        var birthDate = await _dbContext.UserProfiles.AsNoTracking()
            .Where(profile => profile.UserId == userId)
            .Select(profile => profile.BirthDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (birthDate is null || !@event.IsParticipantAgeEligible(birthDate.Value))
        {
            return Result<EventResponse>.Failure(EventErrors.ParticipantAgeNotEligible);
        }

        var utcNow = _timeProvider.GetUtcNow();
        @event.AcceptInvitation(userId, utcNow);
        await EventAccess.AddConversationMemberIfPresentAsync(
            _dbContext, @event.Id, userId, utcNow, cancellationToken);

        var statistics = await _dbContext.UserStatistics
            .FirstOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken);
        statistics?.IncreaseEventsJoined(utcNow);

        if (@event.OrganizerUserId != userId)
        {
            var recipientLanguage = await NotificationActor.ResolveRecipientLanguageAsync(
                _dbContext, @event.OrganizerUserId, cancellationToken);

            await _notificationPublisher.PublishAsync(
                @event.OrganizerUserId,
                NotificationType.EventInvitationAccepted,
                NotificationActor.Format(
                    recipientLanguage,
                    nameof(NotificationsResource.EventInvitationAccepted_Title),
                    await NotificationActor.PrefixAsync(_dbContext, userId, recipientLanguage, cancellationToken)),
                NotificationActor.Format(
                    recipientLanguage,
                    nameof(NotificationsResource.EventInvitationAccepted_Body),
                    @event.Title),
                NotificationEntityType.Event,
                @event.Id,
                userId,
                cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        var response = await EventQueries.GetDetailAsync(
            _dbContext, @event.Id, userId, cancellationToken);
        return Result<EventResponse>.Success(response!);
    }
}
