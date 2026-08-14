using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Social.Friendships.SearchFriends;

public sealed record SearchFriendsQuery(string Q, int Take = 20)
    : IQuery<IReadOnlyList<FriendListItemResponse>>;

public sealed class SearchFriendsQueryValidator : AbstractValidator<SearchFriendsQuery>
{
    public SearchFriendsQueryValidator()
    {
        RuleFor(query => query.Q)
            .NotEmpty()
            .MaximumLength(50);
        RuleFor(query => query.Take).InclusiveBetween(1, 50);
    }
}

internal sealed class SearchFriendsQueryHandler
    : IQueryHandler<SearchFriendsQuery, IReadOnlyList<FriendListItemResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public SearchFriendsQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<FriendListItemResponse>>> Handle(
        SearchFriendsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<IReadOnlyList<FriendListItemResponse>>.Failure(
                FriendshipErrors.NotAuthenticated);
        }

        var term = request.Q.Trim().ToLowerInvariant();

        var query =
            from friendship in _dbContext.Friendships.AsNoTracking()
            where friendship.Status == FriendshipStatus.Accepted
                && (friendship.RequesterUserId == userId || friendship.AddresseeUserId == userId)
            let friendUserId = friendship.RequesterUserId == userId
                ? friendship.AddresseeUserId
                : friendship.RequesterUserId
            join profile in _dbContext.UserProfiles.AsNoTracking()
                on friendUserId equals profile.UserId
            where profile.Username.ToLower().Contains(term)
                || profile.FirstName.ToLower().Contains(term)
                || (profile.LastName != null && profile.LastName.ToLower().Contains(term))
            orderby profile.Username
            select new FriendListItemResponse(
                friendship.Id,
                friendUserId,
                profile.Username,
                profile.FirstName,
                profile.ProfileImageUrl,
                friendship.RespondedAt ?? friendship.CreatedAt);

        var items = await query
            .Take(request.Take)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<FriendListItemResponse>>.Success(items);
    }
}
