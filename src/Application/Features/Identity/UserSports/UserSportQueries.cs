using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;

namespace Sportner.Application.Features.Identity.UserSports;

internal static class UserSportQueries
{
    internal static async Task<IReadOnlyList<UserSportResponse>> GetForUserAsync(
        IApplicationDbContext dbContext,
        Guid userId,
        CancellationToken cancellationToken) =>
        await (from userSport in dbContext.UserSports.AsNoTracking()
               join sport in dbContext.Sports.AsNoTracking() on userSport.SportId equals sport.Id
               where userSport.UserId == userId
               orderby userSport.IsPrimary descending, sport.DisplayOrder
               select new UserSportResponse(
                   sport.Id,
                   sport.Name,
                   sport.Slug,
                   (short)userSport.SkillLevel,
                   userSport.IsPrimary))
            .ToListAsync(cancellationToken);
}
