using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Sportner.Application.Abstractions.Location;
using Sportner.Application.Common.Results;

namespace Sportner.Infrastructure.Location;

/// <summary>
/// OpenStreetMap fallback, used when no Google Places key is configured so the
/// app keeps working locally and before billing is set up.
///
/// Not a like-for-like substitute. The public instance allows roughly one
/// request per second and forbids heavy use, so it cannot carry production
/// traffic. Coverage is uneven too: spot-checking Istanbul venues, exact names
/// ("Burhan Felek Spor Kompleksi", "Caferağa Spor Salonu") resolve, while
/// partial names ("Ataköy Atletizm") and category queries ("tenis kortu
/// beşiktaş") return nothing at all. Degraded mode, not a supported provider.
/// </summary>
internal sealed class NominatimPlaceProvider : IPlaceProvider
{
    // Infrastructure does not reach into the Application error catalog; the
    // handlers translate these into the localized, user-facing messages.
    private static readonly Error LookupFailed = Error.Failure(
        "NominatimPlaceProvider.LookupFailed",
        "The place provider request failed.");

    private const string BaseUrl = "https://nominatim.openstreetmap.org";
    private const int SearchLimit = 6;

    private readonly HttpClient _httpClient;
    private readonly ILogger<NominatimPlaceProvider> _logger;

    public NominatimPlaceProvider(
        HttpClient httpClient,
        ILogger<NominatimPlaceProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<PlaceSuggestion>>> SearchAsync(
        string query,
        string language,
        string? sessionToken,
        CancellationToken cancellationToken)
    {
        var url = $"{BaseUrl}/search"
            + $"?q={Uri.EscapeDataString(query)}"
            + "&format=json&addressdetails=1"
            + $"&limit={SearchLimit}&countrycodes=tr"
            + $"&accept-language={Uri.EscapeDataString(language)}";

        try
        {
            var results = await _httpClient
                .GetFromJsonAsync<List<NominatimResult>>(url, cancellationToken);

            var suggestions = (results ?? [])
                .Select(Map)
                .ToList();

            return Result<IReadOnlyList<PlaceSuggestion>>.Success(suggestions);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException
            && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "Nominatim search request failed.");
            return Result<IReadOnlyList<PlaceSuggestion>>.Failure(LookupFailed);
        }
    }

    /// <summary>
    /// Nominatim has no separate details endpoint — search already returns
    /// coordinates, so the client never needs this round trip. It only runs if
    /// a suggestion somehow arrives without a point.
    /// </summary>
    public Task<Result<ResolvedPlace>> GetDetailsAsync(
        string placeId,
        string language,
        string? sessionToken,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<ResolvedPlace>.Failure(LookupFailed));
    }

    public async Task<Result<ResolvedPlace>> ReverseGeocodeAsync(
        double latitude,
        double longitude,
        string language,
        CancellationToken cancellationToken)
    {
        var url = $"{BaseUrl}/reverse"
            + $"?lat={latitude.ToString(CultureInfo.InvariantCulture)}"
            + $"&lon={longitude.ToString(CultureInfo.InvariantCulture)}"
            + "&format=json&addressdetails=1"
            + $"&accept-language={Uri.EscapeDataString(language)}";

        try
        {
            var result = await _httpClient
                .GetFromJsonAsync<NominatimResult>(url, cancellationToken);

            if (result is null || string.IsNullOrWhiteSpace(result.DisplayName))
            {
                return Result<ResolvedPlace>.Failure(LookupFailed);
            }

            return Result<ResolvedPlace>.Success(
                new ResolvedPlace(result.DisplayName, latitude, longitude));
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException
            && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "Nominatim reverse geocode request failed.");
            return Result<ResolvedPlace>.Failure(LookupFailed);
        }
    }

    private static PlaceSuggestion Map(NominatimResult result)
    {
        var address = result.Address;

        var title = FirstNonEmpty(
            result.Name,
            address?.Road,
            address?.Neighbourhood,
            address?.Suburb,
            address?.Town,
            address?.City)
            ?? result.DisplayName.Split(',').FirstOrDefault()?.Trim()
            ?? result.DisplayName;

        var subtitleParts = new[]
        {
            FirstNonEmpty(address?.Suburb, address?.Neighbourhood),
            FirstNonEmpty(address?.City, address?.Town, address?.District),
            FirstNonEmpty(address?.Province, address?.State),
        }
        .Where(part => !string.IsNullOrWhiteSpace(part))
        .ToList();

        var subtitle = subtitleParts.Count > 0
            ? string.Join(", ", subtitleParts)
            : string.Join(",", result.DisplayName.Split(',').Skip(1).Take(2)).Trim();

        _ = double.TryParse(result.Lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat);
        _ = double.TryParse(result.Lon, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon);

        var id = result.PlaceId.ToString(CultureInfo.InvariantCulture);

        return new PlaceSuggestion(id, id, title, subtitle, result.DisplayName, lat, lon);
    }

    private static string? FirstNonEmpty(params string?[] candidates) =>
        candidates.FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate));

    private sealed record NominatimResult(
        [property: JsonPropertyName("place_id")] long PlaceId,
        [property: JsonPropertyName("display_name")] string DisplayName,
        [property: JsonPropertyName("lat")] string? Lat,
        [property: JsonPropertyName("lon")] string? Lon,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("address")] NominatimAddress? Address);

    private sealed record NominatimAddress(
        [property: JsonPropertyName("road")] string? Road,
        [property: JsonPropertyName("suburb")] string? Suburb,
        [property: JsonPropertyName("neighbourhood")] string? Neighbourhood,
        [property: JsonPropertyName("city")] string? City,
        [property: JsonPropertyName("town")] string? Town,
        [property: JsonPropertyName("district")] string? District,
        [property: JsonPropertyName("province")] string? Province,
        [property: JsonPropertyName("state")] string? State);
}
