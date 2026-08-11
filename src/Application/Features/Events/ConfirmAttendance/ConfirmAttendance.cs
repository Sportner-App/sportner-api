using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Gamification;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Constants;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Events.ConfirmAttendance;

public sealed record ConfirmAttendanceCommand(Guid EventId, Guid UserId) : ICommand<EventResponse>;

internal sealed class ConfirmAttendanceCommandHandler
    : OrganizerEventMutationHandlerBase, ICommandHandler<ConfirmAttendanceCommand, EventResponse>
{
    private readonly IBadgeAwarder _badgeAwarder;

    public ConfirmAttendanceCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        IBadgeAwarder badgeAwarder)
        : base(dbContext, currentUser, timeProvider)
    {
        _badgeAwarder = badgeAwarder;
    }

    public Task<Result<EventResponse>> Handle(
        ConfirmAttendanceCommand request,
        CancellationToken cancellationToken) =>
        MutateAsync(
            request.EventId,
            async (@event, utcNow, ct) =>
            {
                var participant = @event.Participants
                    .FirstOrDefault(candidate => candidate.UserId == request.UserId);

                if (participant is null)
                {
                    return Result.Failure(EventErrors.ParticipantNotFound);
                }

                // Side effects only on Approved → Attended. Domain no-ops when already Attended.
                var shouldCreditAttendance = participant.Status is ParticipantStatus.Approved;

                @event.ConfirmAttendance(request.UserId, utcNow);

                if (!shouldCreditAttendance)
                {
                    return Result.Success();
                }

                var statistics = await DbContext.UserStatistics
                    .FirstOrDefaultAsync(candidate => candidate.UserId == request.UserId, ct);

                statistics?.IncreaseCompletedEvents(utcNow);
                await RefreshAttendanceRateAsync(request.UserId, utcNow, ct);

                await _badgeAwarder.TryAwardAsync(
                    request.UserId,
                    BadgeCodes.FirstEvent,
                    ct);

                await _badgeAwarder.EvaluateAfterAttendanceAsync(request.UserId, ct);

                return Result.Success();
            },
            cancellationToken);

    private async Task RefreshAttendanceRateAsync(
        Guid userId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var statistics = await DbContext.UserStatistics
            .FirstOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken);

        if (statistics is null || statistics.EventsJoined == 0)
        {
            return;
        }

        var attended = await DbContext.EventParticipants.AsNoTracking()
            .CountAsync(
                participant =>
                    participant.UserId == userId
                    && participant.Status == ParticipantStatus.Attended,
                cancellationToken);

        var noShow = await DbContext.EventParticipants.AsNoTracking()
            .CountAsync(
                participant =>
                    participant.UserId == userId
                    && participant.Status == ParticipantStatus.NoShow,
                cancellationToken);

        var decided = attended + noShow;

        if (decided == 0)
        {
            return;
        }

        var rate = attended * 100m / decided;
        statistics.UpdateAttendanceRate(rate, utcNow);
    }
}
