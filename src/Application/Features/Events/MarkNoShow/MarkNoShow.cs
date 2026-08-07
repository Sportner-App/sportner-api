using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Events.MarkNoShow;

public sealed record MarkNoShowCommand(Guid EventId, Guid UserId) : ICommand<EventResponse>;

internal sealed class MarkNoShowCommandHandler
    : OrganizerEventMutationHandlerBase, ICommandHandler<MarkNoShowCommand, EventResponse>
{
    public MarkNoShowCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
        : base(dbContext, currentUser, timeProvider)
    {
    }

    public Task<Result<EventResponse>> Handle(
        MarkNoShowCommand request,
        CancellationToken cancellationToken) =>
        MutateAsync(
            request.EventId,
            async (@event, utcNow, ct) =>
            {
                if (@event.Participants.All(participant => participant.UserId != request.UserId))
                {
                    return Result.Failure(EventErrors.ParticipantNotFound);
                }

                @event.MarkNoShow(request.UserId, utcNow);

                var statistics = await DbContext.UserStatistics
                    .FirstOrDefaultAsync(candidate => candidate.UserId == request.UserId, ct);

                if (statistics is not null)
                {
                    var attended = await DbContext.EventParticipants.AsNoTracking()
                        .CountAsync(
                            participant =>
                                participant.UserId == request.UserId
                                && participant.Status == ParticipantStatus.Attended,
                            ct);

                    // Include the just-marked no-show which is tracked in the aggregate but may
                    // not yet be visible to AsNoTracking queries in the same context.
                    var noShow = await DbContext.EventParticipants
                        .CountAsync(
                            participant =>
                                participant.UserId == request.UserId
                                && participant.Status == ParticipantStatus.NoShow,
                            ct);

                    var decided = attended + noShow;

                    if (decided > 0)
                    {
                        statistics.UpdateAttendanceRate(attended * 100m / decided, utcNow);
                    }
                }

                return Result.Success();
            },
            cancellationToken);
}
