using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Locations.GetPlaceDetails;

public sealed record GetPlaceDetailsQuery(
    string PlaceId,
    string Language,
    string? SessionToken) : IQuery<ResolvedPlaceResponse>;

public sealed record ResolvedPlaceResponse(
    string AddressText,
    double Latitude,
    double Longitude);
