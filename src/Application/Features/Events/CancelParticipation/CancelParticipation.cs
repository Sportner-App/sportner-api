using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Notifications;
using Sportner.Domain.Common.Enums;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Events.CancelParticipation;

public sealed record CancelParticipationCommand(Guid EventId) : ICommand;

internal sealed class CancelParticipationCommandHandler : ICommandHandler<CancelParticipationCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly INotificationPublisher _notificationPublisher;

    public CancelParticipationCommandHandler(
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
        CancelParticipationCommand request,
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

        var participant = @event.Participants.FirstOrDefault(candidate => candidate.UserId == userId);

        if (participant is null)
        {
            return Result.Failure(EventErrors.ParticipantNotFound);
        }

        var wasApproved = participant.Status is ParticipantStatus.Approved;
        var utcNow = _timeProvider.GetUtcNow();

        if (@event.HasEnded(utcNow))
        {
            return Result.Failure(EventErrors.ParticipationLocked);
        }

        @event.CancelParticipation(userId, utcNow);

        if (wasApproved)
        {
            var statistics = await _dbContext.UserStatistics
                .FirstOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken);

            if (statistics is not null && statistics.EventsJoined > 0)
            {
                statistics.DecreaseEventsJoined(utcNow);
            }

            if (@event.OrganizerUserId != userId)
            {
                var recipientLanguage = await NotificationActor.ResolveRecipientLanguageAsync(
                    _dbContext, @event.OrganizerUserId, cancellationToken);

                await _notificationPublisher.PublishAsync(
                    @event.OrganizerUserId,
                    NotificationType.EventParticipationCancelled,
                    NotificationActor.Format(
                        recipientLanguage,
                        nameof(NotificationsResource.EventParticipationCancelled_Title),
                        await NotificationActor.PrefixAsync(
                            _dbContext, userId, recipientLanguage, cancellationToken)),
                    NotificationActor.Format(
                        recipientLanguage,
                        nameof(NotificationsResource.EventParticipationCancelled_Body),
                        @event.Title),
                    NotificationEntityType.Event,
                    @event.Id,
                    userId,
                    cancellationToken);
            }
        }

        await EventAccess.RemoveConversationMemberIfPresentAsync(
            _dbContext,
            @event.Id,
            userId,
            utcNow,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
