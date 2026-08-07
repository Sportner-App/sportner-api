using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Gamification.ListMyBadges;

public sealed record ListMyBadgesQuery : IQuery<IReadOnlyList<UserBadgeResponse>>;

internal sealed class ListMyBadgesQueryHandler
    : IQueryHandler<ListMyBadgesQuery, IReadOnlyList<UserBadgeResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListMyBadgesQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<UserBadgeResponse>>> Handle(
        ListMyBadgesQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<IReadOnlyList<UserBadgeResponse>>.Failure(BadgeErrors.NotAuthenticated);
        }

        var badges = await BadgeQueries.ListForUserAsync(_dbContext, userId, cancellationToken);
        return Result<IReadOnlyList<UserBadgeResponse>>.Success(badges);
    }
}
