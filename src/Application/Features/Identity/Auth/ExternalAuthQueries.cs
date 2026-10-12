using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;

namespace Sportner.Application.Features.Identity.Auth;

internal static class ExternalAuthQueries
{
    /// <summary>
    /// Apple "Hide My Email" adresleri rastgele bir yerel bolum tasiyor
    /// (ornegin <c>x7k2p9@privaterelay.appleid.com</c>); kullanici adi olarak
    /// oneremeyiz.
    /// </summary>
    private const string AppleRelayDomain = "privaterelay.appleid.com";

    public static async Task<string> SuggestUsernameAsync(
        IApplicationDbContext dbContext,
        string? firstName,
        string? lastName,
        string? email,
        string providerUserId,
        CancellationToken cancellationToken)
    {
        var baseName = Sanitize(string.Concat(firstName, lastName));

        if (baseName.Length < 3)
        {
            // Apple ad ve soyadi yalnizca ilk yetkilendirmede donuyor; tekrar
            // giriste elde kalan tek okunabilir ipucu e-posta oluyor.
            baseName = Sanitize(LocalPartOf(email));
        }

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

    private static string Sanitize(string? source) =>
        new((source ?? string.Empty)
            .ToLowerInvariant()
            .Normalize(System.Text.NormalizationForm.FormD)
            .Where(char.IsAsciiLetterOrDigit)
            .ToArray());

    private static string? LocalPartOf(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var separator = email.IndexOf('@');

        if (separator <= 0)
        {
            return null;
        }

        var domain = email[(separator + 1)..];

        if (domain.EndsWith(AppleRelayDomain, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return email[..separator];
    }
}
