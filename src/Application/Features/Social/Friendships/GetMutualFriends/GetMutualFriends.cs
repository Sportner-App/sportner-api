using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Social.Friendships.GetMutualFriends;

public sealed record GetMutualFriendsQuery(Guid UserId, int Take = 20)
    : IQuery<MutualFriendsResponse>;

public sealed class GetMutualFriendsQueryValidator : AbstractValidator<GetMutualFriendsQuery>
{
    public GetMutualFriendsQueryValidator()
    {
        RuleFor(query => query.UserId).NotEmpty();
        RuleFor(query => query.Take).InclusiveBetween(1, 50);
    }
}

internal sealed class GetMutualFriendsQueryHandler
    : IQueryHandler<GetMutualFriendsQuery, MutualFriendsResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetMutualFriendsQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<MutualFriendsResponse>> Handle(
        GetMutualFriendsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } viewerId)
        {
            return Result<MutualFriendsResponse>.Failure(FriendshipErrors.NotAuthenticated);
        }

        var target = await _dbContext.Users.AsNoTracking()
            .Where(user => user.Id == request.UserId)
            .Select(user => new { user.Id, user.Status })
            .FirstOrDefaultAsync(cancellationToken);

        if (target is null
            || target.Status is UserStatus.Deleted or UserStatus.Banned)
        {
            return Result<MutualFriendsResponse>.Failure(FriendshipErrors.UserNotFound);
        }

        if (await BlockQueries.BlockedPairExistsAsync(
                _dbContext,
                viewerId,
                request.UserId,
                cancellationToken))
        {
            return Result<MutualFriendsResponse>.Failure(FriendshipErrors.Blocked);
        }

        if (viewerId != request.UserId)
        {
            var profile = await _dbContext.UserProfiles.AsNoTracking()
                .Where(candidate => candidate.UserId == request.UserId)
                .Select(candidate => new { candidate.IsProfilePublic })
                .FirstOrDefaultAsync(cancellationToken);

            if (profile is null)
            {
                return Result<MutualFriendsResponse>.Failure(FriendshipErrors.UserNotFound);
            }

            if (!profile.IsProfilePublic)
            {
                var areFriends = await SocialQueries.AreAcceptedFriendsAsync(
                    _dbContext,
                    viewerId,
                    request.UserId,
                    cancellationToken);

                if (!areFriends)
                {
                    return Result<MutualFriendsResponse>.Failure(FriendshipErrors.NotVisible);
                }
            }
        }

        var viewerFriends = await SocialQueries.AcceptedFriendIds(_dbContext, viewerId)
            .ToListAsync(cancellationToken);
        var targetFriends = await SocialQueries.AcceptedFriendIds(_dbContext, request.UserId)
            .ToListAsync(cancellationToken);

        var mutualIds = viewerFriends.Intersect(targetFriends).ToList();
        var sampleIds = mutualIds.Take(request.Take).ToList();

        if (sampleIds.Count == 0)
        {
            return Result<MutualFriendsResponse>.Success(
                new MutualFriendsResponse(request.UserId, 0, []));
        }

        var profiles = await _dbContext.UserProfiles.AsNoTracking()
            .Where(profile => sampleIds.Contains(profile.UserId))
            .Select(profile => new MutualFriendItemResponse(
                profile.UserId,
                profile.Username,
                profile.FirstName,
                profile.ProfileImageUrl))
            .ToListAsync(cancellationToken);

        var ordered = sampleIds
            .Select(id => profiles.FirstOrDefault(profile => profile.UserId == id)
                ?? new MutualFriendItemResponse(id, null, null, null))
            .ToList();

        return Result<MutualFriendsResponse>.Success(
            new MutualFriendsResponse(request.UserId, mutualIds.Count, ordered));
    }
}
