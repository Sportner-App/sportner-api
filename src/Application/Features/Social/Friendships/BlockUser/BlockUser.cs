using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Social;

namespace Sportner.Application.Features.Social.Friendships.BlockUser;

public sealed record BlockUserCommand(Guid UserId) : ICommand<FriendshipResponse>;

internal sealed class BlockUserCommandHandler : ICommandHandler<BlockUserCommand, FriendshipResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public BlockUserCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<FriendshipResponse>> Handle(
        BlockUserCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } blockerId)
        {
            return Result<FriendshipResponse>.Failure(FriendshipErrors.NotAuthenticated);
        }

        if (blockerId == request.UserId)
        {
            return Result<FriendshipResponse>.Failure(FriendshipErrors.SelfRequest);
        }

        var targetExists = await _dbContext.Users.AsNoTracking()
            .AnyAsync(user => user.Id == request.UserId, cancellationToken);

        if (!targetExists)
        {
            return Result<FriendshipResponse>.Failure(FriendshipErrors.UserNotFound);
        }

        var utcNow = _timeProvider.GetUtcNow();

        var friendship = await SocialQueries.FindBetweenAsync(
            _dbContext,
            blockerId,
            request.UserId,
            cancellationToken);

        if (friendship is null)
        {
            friendship = Friendship.CreateRequest(blockerId, request.UserId, utcNow);
            _dbContext.Friendships.Add(friendship);
        }

        var wasAccepted = friendship.Status == FriendshipStatus.Accepted;
        friendship.Block(blockerId, utcNow);

        if (wasAccepted)
        {
            foreach (var participantId in new[] { friendship.RequesterUserId, friendship.AddresseeUserId })
            {
                var statistics = await _dbContext.UserStatistics
                    .FirstOrDefaultAsync(candidate => candidate.UserId == participantId, cancellationToken);

                if (statistics is not null && statistics.FriendsCount > 0)
                {
                    statistics.DecreaseFriendsCount(utcNow);
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<FriendshipResponse>.Success(
            await SocialQueries.ToFriendshipResponseAsync(_dbContext, friendship, cancellationToken));
    }
}
