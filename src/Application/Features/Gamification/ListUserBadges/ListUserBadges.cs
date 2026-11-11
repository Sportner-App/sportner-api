using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Features.Identity.UserProfiles;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Gamification.ListUserBadges;

public sealed record ListUserBadgesQuery(Guid UserId) : IQuery<IReadOnlyList<UserBadgeResponse>>;

internal sealed class ListUserBadgesQueryHandler
    : IQueryHandler<ListUserBadgesQuery, IReadOnlyList<UserBadgeResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListUserBadgesQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<UserBadgeResponse>>> Handle(
        ListUserBadgesQuery request,
        CancellationToken cancellationToken)
    {
        var userExists = await _dbContext.Users.AsNoTracking()
            .AnyAsync(user => user.Id == request.UserId, cancellationToken);

        if (!userExists)
        {
            return Result<IReadOnlyList<UserBadgeResponse>>.Failure(BadgeErrors.UserNotFound);
        }

        if (await ProfileQueries.CheckViewAccessAsync(_dbContext, request.UserId, _currentUser.UserId, cancellationToken)
            is { } accessError)
        {
            return Result<IReadOnlyList<UserBadgeResponse>>.Failure(accessError);
        }

        var badges = await BadgeQueries.ListForUserAsync(
            _dbContext,
            request.UserId,
            cancellationToken);

        return Result<IReadOnlyList<UserBadgeResponse>>.Success(badges);
    }
}
