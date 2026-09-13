using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Localization;
using Sportner.Domain.Common.Enums;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Notifications;

/// <summary>
/// Builds the "{actor} …" prefix used at the start of most notification sentences.
/// The prefix itself is language-dependent (Turkish keeps the historical "kullanıcısı" filler
/// word; English reads naturally without it), so callers substitute it into a resx template's
/// <c>{0}</c> placeholder rather than concatenating a raw action phrase.
/// </summary>
internal static class NotificationActor
{
    internal static Task<string?> ResolveUsernameAsync(
        IApplicationDbContext dbContext,
        Guid userId,
        CancellationToken cancellationToken) =>
        dbContext.UserProfiles.AsNoTracking()
            .Where(profile => profile.UserId == userId)
            .Select(profile => profile.Username)
            .FirstOrDefaultAsync(cancellationToken);

    internal static string FormatPrefix(string? username, Language language)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return language == Language.English ? "A user" : "Bir kullanıcı";
        }

        return language == Language.English ? username : $"{username} kullanıcısı";
    }

    /// <summary>Convenience for the common single-recipient case: resolves the actor's username and formats it in one call.</summary>
    internal static async Task<string> PrefixAsync(
        IApplicationDbContext dbContext,
        Guid actorUserId,
        Language language,
        CancellationToken cancellationToken)
    {
        var username = await ResolveUsernameAsync(dbContext, actorUserId, cancellationToken);
        return FormatPrefix(username, language);
    }

    /// <summary>Single-recipient case: the language a notification for this user should render in.</summary>
    internal static Task<Language> ResolveRecipientLanguageAsync(
        IApplicationDbContext dbContext,
        Guid recipientUserId,
        CancellationToken cancellationToken) =>
        dbContext.Users.AsNoTracking()
            .Where(user => user.Id == recipientUserId)
            .Select(user => user.PreferredLanguage)
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>
    /// Multi-recipient case (fan-out notifications): one query instead of N, so a loop over
    /// recipients doesn't issue a separate round trip per person.
    /// </summary>
    internal static async Task<IReadOnlyDictionary<Guid, Language>> ResolveRecipientLanguagesAsync(
        IApplicationDbContext dbContext,
        IReadOnlyCollection<Guid> recipientUserIds,
        CancellationToken cancellationToken)
    {
        if (recipientUserIds.Count == 0)
        {
            return new Dictionary<Guid, Language>();
        }

        return await dbContext.Users.AsNoTracking()
            .Where(user => recipientUserIds.Contains(user.Id))
            .Select(user => new { user.Id, user.PreferredLanguage })
            .ToDictionaryAsync(row => row.Id, row => row.PreferredLanguage, cancellationToken);
    }

    /// <summary>Formats a <see cref="NotificationsResource"/> template in the given language.</summary>
    internal static string Format(Language language, string resourceKey, params object[] args)
    {
        var template = NotificationsResource.ResourceManager.GetString(resourceKey, language.ToCultureInfo())!;
        return string.Format(CultureInfo.InvariantCulture, template, args);
    }
}
