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

    public ListUserBadgesQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
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

        var badges = await BadgeQueries.ListForUserAsync(
            _dbContext,
            request.UserId,
            cancellationToken);

        return Result<IReadOnlyList<UserBadgeResponse>>.Success(badges);
    }
}
