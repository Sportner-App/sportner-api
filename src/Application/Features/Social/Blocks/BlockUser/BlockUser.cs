using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Social;

namespace Sportner.Application.Features.Social.Blocks.BlockUser;

public sealed record BlockUserCommand(Guid UserId) : ICommand<BlockedUserResponse>;

internal sealed class BlockUserCommandHandler : ICommandHandler<BlockUserCommand, BlockedUserResponse>
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

    public async Task<Result<BlockedUserResponse>> Handle(
        BlockUserCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } blockerId)
        {
            return Result<BlockedUserResponse>.Failure(BlockErrors.NotAuthenticated);
        }

        if (blockerId == request.UserId)
        {
            return Result<BlockedUserResponse>.Failure(BlockErrors.SelfBlock);
        }

        var targetExists = await _dbContext.Users.AsNoTracking()
            .AnyAsync(user => user.Id == request.UserId, cancellationToken);

        if (!targetExists)
        {
            return Result<BlockedUserResponse>.Failure(BlockErrors.UserNotFound);
        }

        var utcNow = _timeProvider.GetUtcNow();

        var existingBlock = await _dbContext.UserBlocks
            .FirstOrDefaultAsync(
                block => block.BlockerUserId == blockerId && block.BlockedUserId == request.UserId,
                cancellationToken);

        if (existingBlock is null)
        {
            _dbContext.UserBlocks.Add(UserBlock.Create(blockerId, request.UserId, utcNow));
        }

        var friendship = await SocialQueries.FindBetweenAsync(
            _dbContext,
            blockerId,
            request.UserId,
            cancellationToken);

        if (friendship is not null)
        {
            if (friendship.Status is FriendshipStatus.Accepted)
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

            _dbContext.Friendships.Remove(friendship);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<BlockedUserResponse>.Success(
            await ToResponseAsync(_dbContext, blockerId, request.UserId, cancellationToken));
    }

    private static async Task<BlockedUserResponse> ToResponseAsync(
        IApplicationDbContext dbContext,
        Guid blockerId,
        Guid blockedUserId,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.UserProfiles.AsNoTracking()
            .Where(candidate => candidate.UserId == blockedUserId)
            .Select(candidate => new
            {
                candidate.Username,
                candidate.FirstName,
                candidate.ProfileImageUrl
            })
            .FirstOrDefaultAsync(cancellationToken);

        var createdAt = await dbContext.UserBlocks.AsNoTracking()
            .Where(block => block.BlockerUserId == blockerId && block.BlockedUserId == blockedUserId)
            .Select(block => block.CreatedAt)
            .FirstAsync(cancellationToken);

        return new BlockedUserResponse(
            blockedUserId,
            profile?.Username,
            profile?.FirstName,
            profile?.ProfileImageUrl,
            createdAt);
    }
}
