using System.Globalization;

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
    /// <c>Select</c> projection — <see cref="Resolve"/> is a plain method call and cannot be
    /// translated to SQL, but a captured bool is treated as a query parameter.
    /// </summary>
    public static bool PreferEnglish =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals(
            "en", StringComparison.OrdinalIgnoreCase);

    /// <summary>Use once the entity/row is already materialized in memory (never inside a translated EF query).</summary>
    public static string Resolve(string defaultText, string? englishText) =>
        PreferEnglish && !string.IsNullOrWhiteSpace(englishText) ? englishText! : defaultText;
}
