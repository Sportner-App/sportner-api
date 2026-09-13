using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Gamification;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Quests;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Events.ConfirmAllAttendance;

/// <summary>
/// Organizer's one-tap way to close out attendance for a completed event: everyone still
/// Approved is marked Attended, except whoever is listed in <paramref name="AbsentUserIds"/>
/// (marked No-Show instead). Backs the "Everyone showed up" / "who was missing?" app prompt.
/// </summary>
public sealed record ConfirmAllAttendanceCommand(
    Guid EventId,
    IReadOnlyList<Guid>? AbsentUserIds = null) : ICommand<EventResponse>;

internal sealed class ConfirmAllAttendanceCommandHandler
    : OrganizerEventMutationHandlerBase, ICommandHandler<ConfirmAllAttendanceCommand, EventResponse>
{
    private readonly IBadgeAwarder _badgeAwarder;
    private readonly IQuestProgressTracker _questProgressTracker;
    private readonly INotificationPublisher _notificationPublisher;

    public ConfirmAllAttendanceCommandHandler(
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
        ConfirmAllAttendanceCommand request,
        CancellationToken cancellationToken) =>
        MutateAsync(
            request.EventId,
            async (@event, utcNow, ct) =>
            {
                if (@event.Status is not EventStatus.Completed)
                {
                    return Result.Failure(EventErrors.NotCompleted);
                }

                var absentIds = (request.AbsentUserIds ?? []).ToHashSet();

                var pendingUserIds = @event.Participants
                    .Where(participant =>
                        !participant.IsGuest
                        && participant.UserId is not null
                        && participant.Status == ParticipantStatus.Approved)
                    .Select(participant => participant.UserId!.Value)
                    .ToList();

                foreach (var userId in pendingUserIds)
                {
                    if (absentIds.Contains(userId))
                    {
                        await AttendanceConfirmation.MarkAbsentAsync(DbContext, @event, userId, utcNow, ct);
                    }
                    else
                    {
                        await AttendanceConfirmation.ConfirmAsync(
                            DbContext,
                            @event,
                            userId,
                            _badgeAwarder,
                            _questProgressTracker,
                            _notificationPublisher,
                            utcNow,
                            ct);
                    }
                }

                return Result.Success();
            },
            cancellationToken);
}
