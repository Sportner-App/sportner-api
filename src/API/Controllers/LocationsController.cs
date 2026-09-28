using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sportner.API.Common;
using Sportner.Application.Features.Locations.GetPlaceDetails;
using Sportner.Application.Features.Locations.ReverseGeocode;
using Sportner.Application.Features.Locations.SearchPlaces;

namespace Sportner.API.Controllers;

/// <summary>
/// Address lookup proxied through the API. The provider key stays server-side
/// — shipping it in the app would let anyone spend the project's quota — and
/// the provider can be swapped without a client release.
///
/// Authorized on purpose: every call costs money, and the only screens that
/// search for an address are behind login anyway.
/// </summary>
[Authorize]
public sealed class LocationsController : ApiControllerBase
{
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] string? language,
        [FromQuery] string? sessionToken,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new SearchPlacesQuery(q ?? string.Empty, ResolveLanguage(language), sessionToken),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpGet("places/{placeId}")]
    public async Task<IActionResult> Details(
        string placeId,
        [FromQuery] string? language,
        [FromQuery] string? sessionToken,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new GetPlaceDetailsQuery(placeId, ResolveLanguage(language), sessionToken),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpGet("reverse")]
    public async Task<IActionResult> Reverse(
        [FromQuery] double lat,
        [FromQuery] double lng,
        [FromQuery] string? language,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new ReverseGeocodeQuery(lat, lng, ResolveLanguage(language)),
            cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Providers expect a plain two-letter code; Turkish is the default audience.</summary>
    private static string ResolveLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return "tr";
        }

        var trimmed = language.Trim();

        return trimmed.Length >= 2 ? trimmed[..2].ToLowerInvariant() : "tr";
    }
}
