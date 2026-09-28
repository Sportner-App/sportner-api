using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Features.Locations.GetPlaceDetails;

namespace Sportner.Application.Features.Locations.ReverseGeocode;

public sealed record ReverseGeocodeQuery(
    double Latitude,
    double Longitude,
    string Language) : IQuery<ResolvedPlaceResponse>;
