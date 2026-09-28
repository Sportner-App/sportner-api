using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sportner.Application.Abstractions.Location;
using Sportner.Application.Common.Results;

namespace Sportner.Infrastructure.Location;

/// <summary>
/// Places API (New) plus the Geocoding API for reverse lookups.
///
/// The legacy Places API is not an option: it went Legacy on 2025-03-01 and is
/// not available in Cloud projects created after that date.
///
/// Places API (New) bills per requested field, so every call sends the
/// narrowest possible X-Goog-FieldMask. Widening a mask changes what the
/// request costs, so treat these as pricing decisions rather than plumbing.
/// </summary>
internal sealed class GooglePlaceProvider : IPlaceProvider
{
    // Infrastructure does not reach into the Application error catalog; the
    // handlers translate these into the localized, user-facing messages.
    private static readonly Error LookupFailed = Error.Failure(
        "GooglePlaceProvider.LookupFailed",
        "The place provider request failed.");

    private const string PlacesBaseUrl = "https://places.googleapis.com/v1";
    private const string GeocodeUrl = "https://maps.googleapis.com/maps/api/geocode/json";

    private const string AutocompleteFieldMask =
        "suggestions.placePrediction.placeId,"
        + "suggestions.placePrediction.text,"
        + "suggestions.placePrediction.structuredFormat";

    private const string DetailsFieldMask = "formattedAddress,location,displayName";

    private readonly HttpClient _httpClient;
    private readonly GooglePlacesOptions _options;
    private readonly ILogger<GooglePlaceProvider> _logger;

    public GooglePlaceProvider(
        HttpClient httpClient,
        IOptions<GooglePlacesOptions> options,
        ILogger<GooglePlaceProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<PlaceSuggestion>>> SearchAsync(
        string query,
        string language,
        string? sessionToken,
        CancellationToken cancellationToken)
    {
        var request = new AutocompleteRequest(
            query,
            language,
            _options.RegionCode,
            [_options.RegionCode],
            sessionToken);

        try
        {
            using var message = new HttpRequestMessage(
                HttpMethod.Post,
                $"{PlacesBaseUrl}/places:autocomplete")
            {
                Content = JsonContent.Create(request),
            };

            message.Headers.Add("X-Goog-Api-Key", _options.ApiKey);
            message.Headers.Add("X-Goog-FieldMask", AutocompleteFieldMask);

            using var response = await _httpClient.SendAsync(message, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                await LogFailureAsync("autocomplete", response, cancellationToken);
                return Result<IReadOnlyList<PlaceSuggestion>>.Failure(
                    LookupFailed);
            }

            var payload = await response.Content
                .ReadFromJsonAsync<AutocompleteResponse>(cancellationToken);

            var suggestions = (payload?.Suggestions ?? [])
                .Select(entry => entry.PlacePrediction)
                .Where(prediction => prediction is not null
                    && !string.IsNullOrWhiteSpace(prediction.PlaceId))
                .Select(prediction =>
                {
                    var full = prediction!.Text?.Text ?? string.Empty;
                    var main = prediction.StructuredFormat?.MainText?.Text;
                    var secondary = prediction.StructuredFormat?.SecondaryText?.Text;

                    return new PlaceSuggestion(
                        prediction.PlaceId!,
                        prediction.PlaceId!,
                        // Falling back to the first comma-separated part keeps the
                        // list readable when Google returns no structured format.
                        string.IsNullOrWhiteSpace(main)
                            ? full.Split(',').FirstOrDefault()?.Trim() ?? full
                            : main,
                        string.IsNullOrWhiteSpace(secondary) ? full : secondary,
                        full,
                        // Autocomplete carries no coordinates; the details call
                        // resolves them once the user picks a suggestion.
                        null,
                        null);
                })
                .ToList();

            return Result<IReadOnlyList<PlaceSuggestion>>.Success(suggestions);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException
            && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "Google Places autocomplete request failed.");
            return Result<IReadOnlyList<PlaceSuggestion>>.Failure(LookupFailed);
        }
    }

    public async Task<Result<ResolvedPlace>> GetDetailsAsync(
        string placeId,
        string language,
        string? sessionToken,
        CancellationToken cancellationToken)
    {
        var url = $"{PlacesBaseUrl}/places/{Uri.EscapeDataString(placeId)}"
            + $"?languageCode={Uri.EscapeDataString(language)}";

        if (!string.IsNullOrWhiteSpace(sessionToken))
        {
            url += $"&sessionToken={Uri.EscapeDataString(sessionToken)}";
        }

        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Get, url);
            message.Headers.Add("X-Goog-Api-Key", _options.ApiKey);
            message.Headers.Add("X-Goog-FieldMask", DetailsFieldMask);

            using var response = await _httpClient.SendAsync(message, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                await LogFailureAsync("place details", response, cancellationToken);
                return Result<ResolvedPlace>.Failure(LookupFailed);
            }

            var payload = await response.Content
                .ReadFromJsonAsync<PlaceDetailsResponse>(cancellationToken);

            if (payload?.Location is null)
            {
                return Result<ResolvedPlace>.Failure(LookupFailed);
            }

            var address = payload.FormattedAddress
                ?? payload.DisplayName?.Text
                ?? string.Empty;

            return Result<ResolvedPlace>.Success(new ResolvedPlace(
                address,
                payload.Location.Latitude,
                payload.Location.Longitude));
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException
            && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "Google place details request failed.");
            return Result<ResolvedPlace>.Failure(LookupFailed);
        }
    }

    public async Task<Result<ResolvedPlace>> ReverseGeocodeAsync(
        double latitude,
        double longitude,
        string language,
        CancellationToken cancellationToken)
    {
        // Reverse lookups stay on the Geocoding API: it is not a legacy service
        // and returns a formatted address for an arbitrary point, which the
        // Places endpoints do not.
        var url = $"{GeocodeUrl}"
            + $"?latlng={latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
            + $",{longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
            + $"&language={Uri.EscapeDataString(language)}"
            + $"&key={Uri.EscapeDataString(_options.ApiKey)}";

        try
        {
            var payload = await _httpClient.GetFromJsonAsync<GeocodeResponse>(url, cancellationToken);

            if (payload?.Status != "OK" || payload.Results is not { Count: > 0 })
            {
                _logger.LogWarning(
                    "Google reverse geocode returned {Status}.",
                    payload?.Status ?? "no response");

                return Result<ResolvedPlace>.Failure(LookupFailed);
            }

            return Result<ResolvedPlace>.Success(new ResolvedPlace(
                payload.Results[0].FormattedAddress ?? string.Empty,
                latitude,
                longitude));
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException
            && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "Google reverse geocode request failed.");
            return Result<ResolvedPlace>.Failure(LookupFailed);
        }
    }

    /// <summary>Google puts the useful part in the body, so log it rather than the status alone.</summary>
    private async Task LogFailureAsync(
        string operation,
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        _logger.LogWarning(
            "Google Places {Operation} failed with {StatusCode}: {Body}",
            operation,
            (int)response.StatusCode,
            body);
    }

    private sealed record AutocompleteRequest(
        [property: JsonPropertyName("input")] string Input,
        [property: JsonPropertyName("languageCode")] string LanguageCode,
        [property: JsonPropertyName("regionCode")] string RegionCode,
        [property: JsonPropertyName("includedRegionCodes")] IReadOnlyList<string> IncludedRegionCodes,
        [property: JsonPropertyName("sessionToken")] string? SessionToken);

    private sealed record AutocompleteResponse(
        [property: JsonPropertyName("suggestions")] IReadOnlyList<SuggestionEntry>? Suggestions);

    private sealed record SuggestionEntry(
        [property: JsonPropertyName("placePrediction")] PlacePrediction? PlacePrediction);

    private sealed record PlacePrediction(
        [property: JsonPropertyName("placeId")] string? PlaceId,
        [property: JsonPropertyName("text")] TextValue? Text,
        [property: JsonPropertyName("structuredFormat")] StructuredFormat? StructuredFormat);

    private sealed record StructuredFormat(
        [property: JsonPropertyName("mainText")] TextValue? MainText,
        [property: JsonPropertyName("secondaryText")] TextValue? SecondaryText);

    private sealed record TextValue(
        [property: JsonPropertyName("text")] string? Text);

    private sealed record PlaceDetailsResponse(
        [property: JsonPropertyName("formattedAddress")] string? FormattedAddress,
        [property: JsonPropertyName("location")] LatLng? Location,
        [property: JsonPropertyName("displayName")] TextValue? DisplayName);

    private sealed record LatLng(
        [property: JsonPropertyName("latitude")] double Latitude,
        [property: JsonPropertyName("longitude")] double Longitude);

    private sealed record GeocodeResponse(
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("results")] IReadOnlyList<GeocodeResult>? Results);

    private sealed record GeocodeResult(
        [property: JsonPropertyName("formatted_address")] string? FormattedAddress);
}
