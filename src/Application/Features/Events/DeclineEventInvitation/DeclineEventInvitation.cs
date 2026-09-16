using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Notifications;
using Sportner.Domain.Common.Enums;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Events.DeclineEventInvitation;

public sealed record DeclineEventInvitationCommand(Guid EventId) : ICommand;

internal sealed class DeclineEventInvitationCommandHandler
    : ICommandHandler<DeclineEventInvitationCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly INotificationPublisher _notificationPublisher;

    public DeclineEventInvitationCommandHandler(
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

    public async Task<Result> Handle(
        DeclineEventInvitationCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result.Failure(EventErrors.NotAuthenticated);
        }

        var @event = await EventAccess.LoadAggregateAsync(_dbContext, request.EventId, cancellationToken);
        if (@event is null)
        {
            return Result.Failure(EventErrors.NotFound);
        }

        var participant = @event.Participants.FirstOrDefault(item => item.UserId == userId);
        if (participant?.Status is not Domain.Common.Enums.ParticipantStatus.Invited)
        {
            return Result.Failure(EventErrors.InvitationNotFound);
        }

        @event.DeclineInvitation(userId, _timeProvider.GetUtcNow());

        if (@event.OrganizerUserId != userId)
        {
            var recipientLanguage = await NotificationActor.ResolveRecipientLanguageAsync(
                _dbContext, @event.OrganizerUserId, cancellationToken);

            await _notificationPublisher.PublishAsync(
                @event.OrganizerUserId,
                NotificationType.EventInvitationDeclined,
                NotificationActor.Format(
                    recipientLanguage,
                    nameof(NotificationsResource.EventInvitationDeclined_Title),
                    await NotificationActor.PrefixAsync(_dbContext, userId, recipientLanguage, cancellationToken)),
                NotificationActor.Format(
                    recipientLanguage,
                    nameof(NotificationsResource.EventInvitationDeclined_Body),
                    @event.Title),
                NotificationEntityType.Event,
                @event.Id,
                userId,
                cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
