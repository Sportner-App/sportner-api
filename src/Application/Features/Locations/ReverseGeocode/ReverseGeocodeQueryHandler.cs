using Sportner.Application.Abstractions.Location;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Locations;
using Sportner.Application.Features.Locations.GetPlaceDetails;

namespace Sportner.Application.Features.Locations.ReverseGeocode;

internal sealed class ReverseGeocodeQueryHandler
    : IQueryHandler<ReverseGeocodeQuery, ResolvedPlaceResponse>
{
    private readonly IPlaceProvider _placeProvider;

    public ReverseGeocodeQueryHandler(IPlaceProvider placeProvider)
    {
        _placeProvider = placeProvider;
    }

    public async Task<Result<ResolvedPlaceResponse>> Handle(
        ReverseGeocodeQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _placeProvider.ReverseGeocodeAsync(
            request.Latitude,
            request.Longitude,
            request.Language,
            cancellationToken);

        if (result.IsFailure)
        {
            return Result<ResolvedPlaceResponse>.Failure(LocationErrors.ReverseFailed);
        }

        var place = result.Value!;

        return Result<ResolvedPlaceResponse>.Success(
            new ResolvedPlaceResponse(place.AddressText, place.Latitude, place.Longitude));
    }
}
