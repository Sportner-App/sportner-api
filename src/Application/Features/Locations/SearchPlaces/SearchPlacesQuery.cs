using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Locations.SearchPlaces;

/// <param name="SessionToken">
/// Ties the keystrokes of one address pick to the details call that follows,
/// so the provider bills them as a single session. The client generates it and
/// reuses it until a suggestion is chosen.
/// </param>
public sealed record SearchPlacesQuery(
    string Query,
    string Language,
    string? SessionToken) : IQuery<IReadOnlyList<PlaceSuggestionResponse>>;

public sealed record PlaceSuggestionResponse(
    string Id,
    string PlaceId,
    string Title,
    string Subtitle,
    string AddressText,
    double? Latitude,
    double? Longitude);
