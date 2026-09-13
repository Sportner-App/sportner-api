using System.Globalization;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Common.Localization;

/// <summary>
/// Resolves which language reference-data text (sport/badge/quest/report-reason names, etc.)
/// should render in, based on the current request's negotiated UI culture (see
/// LocalizationExtension.AddCustomLocalization — set from the Accept-Language header).
/// </summary>
public static class CatalogLocalization
{
    /// <summary>
    /// Captured into a local variable before an EF query so it can be used inside a
    /// <c>Select</c> projection — <see cref="Resolve(string,string?)"/> is a plain method call and cannot be
    /// translated to SQL, but a captured bool is treated as a query parameter.
    /// </summary>
    public static bool PreferEnglish =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals(
            "en", StringComparison.OrdinalIgnoreCase);

    /// <summary>The current request's negotiated UI culture, as a stored <see cref="Language"/> value.</summary>
    public static Language CurrentLanguage => PreferEnglish ? Language.English : Language.Turkish;

    /// <summary>Use once the entity/row is already materialized in memory (never inside a translated EF query).</summary>
    public static string Resolve(string defaultText, string? englishText) =>
        PreferEnglish && !string.IsNullOrWhiteSpace(englishText) ? englishText! : defaultText;

    /// <summary>
    /// Resolves text for an explicit target <paramref name="language"/> rather than the current
    /// request's culture — use this for content addressed to a DIFFERENT user (e.g. a
    /// notification), where the ambient request culture reflects the actor, not the recipient,
    /// or there may be no HTTP request at all (background jobs).
    /// </summary>
    public static string Resolve(Language language, string defaultText, string? englishText) =>
        language == Language.English && !string.IsNullOrWhiteSpace(englishText) ? englishText! : defaultText;

    public static CultureInfo ToCultureInfo(this Language language) =>
        language == Language.English
            ? CultureInfo.GetCultureInfo("en-US")
            : CultureInfo.GetCultureInfo("tr-TR");
}
