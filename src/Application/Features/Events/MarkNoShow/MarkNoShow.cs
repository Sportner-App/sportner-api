using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

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

                await AttendanceConfirmation.MarkAbsentAsync(DbContext, @event, request.UserId, utcNow, ct);

                return Result.Success();
            },
            cancellationToken);
}
