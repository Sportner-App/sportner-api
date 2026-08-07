using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Gamification.ListBadges;

public sealed record ListBadgesQuery : IQuery<IReadOnlyList<BadgeResponse>>;

internal sealed class ListBadgesQueryHandler
    : IQueryHandler<ListBadgesQuery, IReadOnlyList<BadgeResponse>>
{
    private readonly IApplicationDbContext _dbContext;

    public ListBadgesQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<BadgeResponse>>> Handle(
        ListBadgesQuery request,
        CancellationToken cancellationToken)
    {
        var badges = await _dbContext.Badges.AsNoTracking()
            .Where(badge => badge.IsActive)
            .OrderBy(badge => badge.DisplayOrder)
            .ThenBy(badge => badge.Code)
            .Select(badge => new BadgeResponse(
                badge.Id,
                badge.Code,
                badge.Name,
                badge.Description,
                badge.IconPath,
                (short)badge.Category,
                (short)badge.Rarity,
                badge.ExperiencePoints,
                badge.DisplayOrder))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<BadgeResponse>>.Success(badges);
    }
}
