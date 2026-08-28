using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Events.DeclineEventInvitation;

public sealed record DeclineEventInvitationCommand(Guid EventId) : ICommand;

internal sealed class DeclineEventInvitationCommandHandler
    : ICommandHandler<DeclineEventInvitationCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public DeclineEventInvitationCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
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
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
