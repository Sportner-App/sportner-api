using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Gamification;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Quests;

namespace Sportner.Application.Features.Events.ConfirmAttendance;

public sealed record ConfirmAttendanceCommand(Guid EventId, Guid UserId) : ICommand<EventResponse>;

internal sealed class ConfirmAttendanceCommandHandler
    : OrganizerEventMutationHandlerBase, ICommandHandler<ConfirmAttendanceCommand, EventResponse>
{
    private readonly IBadgeAwarder _badgeAwarder;
    private readonly IQuestProgressTracker _questProgressTracker;
    private readonly INotificationPublisher _notificationPublisher;

    public ConfirmAttendanceCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        IBadgeAwarder badgeAwarder,
        IQuestProgressTracker questProgressTracker,
        INotificationPublisher notificationPublisher)
        : base(dbContext, currentUser, timeProvider)
    {
        _badgeAwarder = badgeAwarder;
        _questProgressTracker = questProgressTracker;
        _notificationPublisher = notificationPublisher;
    }

    public Task<Result<EventResponse>> Handle(
        ConfirmAttendanceCommand request,
        CancellationToken cancellationToken) =>
        MutateAsync(
            request.EventId,
            async (@event, utcNow, ct) =>
            {
                if (@event.Participants.All(participant => participant.UserId != request.UserId))
                {
                    return Result.Failure(EventErrors.ParticipantNotFound);
                }

                await AttendanceConfirmation.ConfirmAsync(
                    DbContext,
                    @event,
                    request.UserId,
                    _badgeAwarder,
                    _questProgressTracker,
                    _notificationPublisher,
                    utcNow,
                    ct);

                return Result.Success();
            },
            cancellationToken);
}
