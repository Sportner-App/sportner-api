using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

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
                var wasAlreadyCancelled = @event.Status == EventStatus.Cancelled;

                @event.Cancel(utcNow);

                if (!wasAlreadyCancelled)
                {
                    await EventAccess.CloseEventConversationAsync(DbContext, @event.Id, utcNow, ct);

                    var recipients = @event.Participants
                        .Where(participant =>
                            participant.UserId is { } userId
                            && userId != @event.OrganizerUserId
                            && participant.Status is ParticipantStatus.Pending
                                or ParticipantStatus.Approved)
                        .Select(participant => participant.UserId!.Value)
                        .Distinct();

                    foreach (var recipientId in recipients)
                    {
                        await _notificationPublisher.PublishAsync(
                            recipientId,
                            NotificationType.EventCancelled,
                            "Etkinlik iptal edildi",
                            $"\"{@event.Title}\" etkinliği iptal edildi.",
                            NotificationEntityType.Event,
                            @event.Id,
                            @event.OrganizerUserId,
                            ct);
                    }

                    var organizerStatistics = await DbContext.UserStatistics
                        .FirstOrDefaultAsync(
                            candidate => candidate.UserId == @event.OrganizerUserId,
                            ct);

                    organizerStatistics?.IncreaseCancelledEvents(utcNow);

                    // Approved attendees had EventsJoined bumped on approve/promote — reverse it.
                    var approvedAttendeeIds = @event.Participants
                        .Where(participant =>
                            participant.UserId is { } userId
                            && userId != @event.OrganizerUserId
                            && participant.Status is ParticipantStatus.Approved)
                        .Select(participant => participant.UserId!.Value)
                        .Distinct()
                        .ToList();

                    foreach (var attendeeId in approvedAttendeeIds)
                    {
                        var attendeeStatistics = await DbContext.UserStatistics
                            .FirstOrDefaultAsync(candidate => candidate.UserId == attendeeId, ct);

                        if (attendeeStatistics is not null && attendeeStatistics.EventsJoined > 0)
                        {
                            attendeeStatistics.DecreaseEventsJoined(utcNow);
                        }
                    }
                }

                return Result.Success();
            },
            cancellationToken);
}
