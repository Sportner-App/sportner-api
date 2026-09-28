namespace Sportner.Infrastructure.Location;

/// <summary>
/// Bound from the <c>GooglePlaces</c> configuration section. When
/// <see cref="ApiKey"/> is empty, dependency injection falls back to the
/// Nominatim provider instead — same pattern as the email sender, so address
/// search keeps working locally and before billing is set up.
/// </summary>
public sealed class GooglePlacesOptions
{
    public const string SectionName = "GooglePlaces";

    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Restricts suggestions to one country. Sportner only runs in Turkey, and
    /// the narrower search returns far better local results.
    /// </summary>
    public string RegionCode { get; set; } = "tr";
}
