using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Social.Friendships.RemoveFriendship;

public sealed record RemoveFriendshipCommand(Guid FriendshipId) : ICommand;

internal sealed class RemoveFriendshipCommandHandler : ICommandHandler<RemoveFriendshipCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public RemoveFriendshipCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(RemoveFriendshipCommand request, CancellationToken cancellationToken)
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

        if (!friendship.InvolvesUser(userId))
        {
            return Result.Failure(FriendshipErrors.NotParticipant);
        }

        if (friendship.Status is not FriendshipStatus.Accepted)
        {
            return Result.Failure(FriendshipErrors.NotAccepted);
        }

        var utcNow = _timeProvider.GetUtcNow();

        foreach (var participantId in new[] { friendship.RequesterUserId, friendship.AddresseeUserId })
        {
            var statistics = await _dbContext.UserStatistics
                .FirstOrDefaultAsync(candidate => candidate.UserId == participantId, cancellationToken);

            if (statistics is not null && statistics.FriendsCount > 0)
            {
                statistics.DecreaseFriendsCount(utcNow);
            }
        }

        _dbContext.Friendships.Remove(friendship);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
