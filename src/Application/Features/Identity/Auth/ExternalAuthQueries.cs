using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;

namespace Sportner.Application.Features.Identity.Auth;

internal static class ExternalAuthQueries
{
    public static async Task<string> SuggestUsernameAsync(
        IApplicationDbContext dbContext,
        string? firstName,
        string? lastName,
        string providerUserId,
        CancellationToken cancellationToken)
    {
        var source = string.Concat(firstName, lastName);
        var baseName = new string(source
            .ToLowerInvariant()
            .Normalize(System.Text.NormalizationForm.FormD)
            .Where(character => char.IsAsciiLetterOrDigit(character))
            .ToArray());

        if (baseName.Length < 3)
        {
            var suffix = new string(providerUserId.Where(char.IsAsciiLetterOrDigit).TakeLast(6).ToArray());
            baseName = $"sportner{suffix}".ToLowerInvariant();
        }

        baseName = baseName[..Math.Min(baseName.Length, 26)];
        var usernames = await dbContext.UserProfiles
            .AsNoTracking()
            .Where(profile => profile.Username.StartsWith(baseName))
            .Select(profile => profile.Username)
            .ToListAsync(cancellationToken);
        var existing = usernames.ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!existing.Contains(baseName))
        {
            return baseName;
        }

        for (var suffix = 2; suffix < 10_000; suffix++)
        {
            var candidate = $"{baseName[..Math.Min(baseName.Length, 30 - suffix.ToString().Length)]}{suffix}";
            if (!existing.Contains(candidate))
            {
                return candidate;
            }
        }

        return $"sportner{Guid.NewGuid():N}"[..30];
    }
}
