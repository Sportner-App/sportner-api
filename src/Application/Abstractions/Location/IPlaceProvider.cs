using Sportner.Application.Common.Results;

namespace Sportner.Application.Abstractions.Location;

/// <summary>A place the user can pick from the search list.</summary>
/// <param name="Id">Stable id for list keys; same value as <paramref name="PlaceId"/>.</param>
/// <param name="PlaceId">Passed back to resolve coordinates.</param>
/// <param name="Latitude">Set only when the provider already knows the point,
/// which lets the client skip the details call. Google fills these in a second
/// request; Nominatim returns them with the search results.</param>
public sealed record PlaceSuggestion(
    string Id,
    string PlaceId,
    string Title,
    string Subtitle,
    string AddressText,
    double? Latitude,
    double? Longitude);

public sealed record ResolvedPlace(
    string AddressText,
    double Latitude,
    double Longitude);

/// <summary>
/// Address lookup, kept behind the API so the provider key never ships in the
/// app and the provider can be swapped without a client release.
/// </summary>
public interface IPlaceProvider
{
    /// <param name="sessionToken">
    /// Groups the keystrokes of one address pick with the details call that
    /// follows it. Google bills those as a single session instead of per
    /// request, so it must be the same value for both and new for the next
    /// pick. Providers that have no such concept ignore it.
    /// </param>
    Task<Result<IReadOnlyList<PlaceSuggestion>>> SearchAsync(
        string query,
        string language,
        string? sessionToken,
        CancellationToken cancellationToken);

    Task<Result<ResolvedPlace>> GetDetailsAsync(
        string placeId,
        string language,
        string? sessionToken,
        CancellationToken cancellationToken);

    Task<Result<ResolvedPlace>> ReverseGeocodeAsync(
        double latitude,
        double longitude,
        string language,
        CancellationToken cancellationToken);
}
