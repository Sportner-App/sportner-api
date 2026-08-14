using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Social.Friendships.ListPendingRequests;

public sealed record ListPendingRequestsQuery(bool Outgoing = false)
    : IQuery<IReadOnlyList<FriendshipResponse>>;

internal sealed class ListPendingRequestsQueryHandler
    : IQueryHandler<ListPendingRequestsQuery, IReadOnlyList<FriendshipResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListPendingRequestsQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<FriendshipResponse>>> Handle(
        ListPendingRequestsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<IReadOnlyList<FriendshipResponse>>.Failure(
                FriendshipErrors.NotAuthenticated);
        }

        var friendships = await _dbContext.Friendships.AsNoTracking()
            .Where(friendship =>
                friendship.Status == FriendshipStatus.Pending
                && (request.Outgoing
                    ? friendship.RequesterUserId == userId
                    : friendship.AddresseeUserId == userId))
            .OrderByDescending(friendship => friendship.CreatedAt)
            .ToListAsync(cancellationToken);

        if (friendships.Count == 0)
        {
            return Result<IReadOnlyList<FriendshipResponse>>.Success([]);
        }

        var otherUserIds = friendships
            .Select(friendship =>
                friendship.RequesterUserId == userId
                    ? friendship.AddresseeUserId
                    : friendship.RequesterUserId)
            .Distinct()
            .ToList();

        var mutualCounts = await SocialQueries.CountMutualFriendsAsync(
            _dbContext,
            userId,
            otherUserIds,
            cancellationToken);
        var sharedSports = await SocialQueries.GetSharedSportNamesAsync(
            _dbContext,
            userId,
            otherUserIds,
            cancellationToken);

        var responses = new List<FriendshipResponse>(friendships.Count);

        foreach (var friendship in friendships)
        {
            var baseResponse = await SocialQueries.ToFriendshipResponseAsync(
                _dbContext,
                friendship,
                cancellationToken);

            var otherUserId = friendship.RequesterUserId == userId
                ? friendship.AddresseeUserId
                : friendship.RequesterUserId;

            responses.Add(baseResponse with
            {
                MutualFriendsCount = mutualCounts.GetValueOrDefault(otherUserId),
                SharedSportNames = sharedSports.GetValueOrDefault(otherUserId) ?? []
            });
        }

        return Result<IReadOnlyList<FriendshipResponse>>.Success(responses);
    }
}
