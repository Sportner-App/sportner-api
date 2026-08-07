using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Events.CompleteEvent;

public sealed record CompleteEventCommand(Guid EventId) : ICommand<EventResponse>;

internal sealed class CompleteEventCommandHandler
    : OrganizerEventMutationHandlerBase, ICommandHandler<CompleteEventCommand, EventResponse>
{
    public CompleteEventCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
        : base(dbContext, currentUser, timeProvider)
    {
    }

    public Task<Result<EventResponse>> Handle(
        CompleteEventCommand request,
        CancellationToken cancellationToken) =>
        MutateAsync(
            request.EventId,
            async (@event, utcNow, ct) =>
            {
                @event.Complete(utcNow);
                await EventAccess.CloseEventConversationAsync(DbContext, @event.Id, utcNow, ct);
                return Result.Success();
            },
            cancellationToken);
}
