using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Models;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Social.Friendships.ListFriends;

public sealed record ListFriendsQuery(int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<FriendListItemResponse>>;

internal sealed class ListFriendsQueryHandler
    : IQueryHandler<ListFriendsQuery, PagedResult<FriendListItemResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListFriendsQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<FriendListItemResponse>>> Handle(
        ListFriendsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<PagedResult<FriendListItemResponse>>.Failure(
                FriendshipErrors.NotAuthenticated);
        }

        var pagination = new PaginationRequest(request.Page, request.PageSize);

        var query =
            from friendship in _dbContext.Friendships.AsNoTracking()
            where friendship.Status == FriendshipStatus.Accepted
                && (friendship.RequesterUserId == userId || friendship.AddresseeUserId == userId)
            let friendUserId = friendship.RequesterUserId == userId
                ? friendship.AddresseeUserId
                : friendship.RequesterUserId
            join profile in _dbContext.UserProfiles.AsNoTracking()
                on friendUserId equals profile.UserId into profiles
            from profile in profiles.DefaultIfEmpty()
            orderby friendship.RespondedAt descending, friendship.CreatedAt descending
            select new FriendListItemResponse(
                friendship.Id,
                friendUserId,
                profile != null ? profile.Username : null,
                profile != null ? profile.FirstName : null,
                profile != null ? profile.ProfileImageUrl : null,
                friendship.RespondedAt ?? friendship.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip(pagination.Skip)
            .Take(pagination.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        return Result<PagedResult<FriendListItemResponse>>.Success(
            PagedResult<FriendListItemResponse>.Create(
                items,
                pagination.NormalizedPage,
                pagination.NormalizedPageSize,
                total));
    }
}
