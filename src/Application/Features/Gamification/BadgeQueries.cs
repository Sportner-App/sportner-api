using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;

namespace Sportner.Application.Features.Gamification;

internal static class BadgeQueries
{
    internal static async Task<IReadOnlyList<UserBadgeResponse>> ListForUserAsync(
        IApplicationDbContext dbContext,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await (
                from userBadge in dbContext.UserBadges.AsNoTracking()
                join badge in dbContext.Badges.AsNoTracking()
                    on userBadge.BadgeId equals badge.Id
                where userBadge.UserId == userId && badge.IsActive
                orderby userBadge.EarnedAt descending
                select new UserBadgeResponse(
                    userBadge.Id,
                    badge.Id,
                    badge.Code,
                    badge.Name,
                    badge.Description,
                    badge.IconPath,
                    (short)badge.Category,
                    (short)badge.Rarity,
                    badge.ExperiencePoints,
                    userBadge.EarnedAt))
            .ToListAsync(cancellationToken);
    }
}
