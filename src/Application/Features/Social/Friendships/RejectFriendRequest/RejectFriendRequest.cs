using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Social.Friendships.RejectFriendRequest;

public sealed record RejectFriendRequestCommand(Guid FriendshipId) : ICommand;

internal sealed class RejectFriendRequestCommandHandler : ICommandHandler<RejectFriendRequestCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public RejectFriendRequestCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(RejectFriendRequestCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result.Failure(FriendshipErrors.NotAuthenticated);
        }

        var friendship = await _dbContext.Friendships
            .FirstOrDefaultAsync(candidate => candidate.Id == request.FriendshipId, cancellationToken);

        if (friendship is null)
        {
            return Result.Failure(FriendshipErrors.NotFound);
        }

        if (friendship.AddresseeUserId != userId)
        {
            return Result.Failure(FriendshipErrors.NotAddressee);
        }

        friendship.Reject(_timeProvider.GetUtcNow());
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
