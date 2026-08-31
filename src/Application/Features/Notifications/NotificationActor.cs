using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;

namespace Sportner.Application.Features.Notifications;

internal static class NotificationActor
{
    internal static async Task<string> TitleAsync(
        IApplicationDbContext dbContext,
        Guid userId,
        string action,
        CancellationToken cancellationToken)
    {
        var username = await dbContext.UserProfiles.AsNoTracking()
            .Where(profile => profile.UserId == userId)
            .Select(profile => profile.Username)
            .FirstOrDefaultAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(username)
            ? $"Bir kullanıcı {action}"
            : $"{username} kullanıcısı {action}";
    }
}
