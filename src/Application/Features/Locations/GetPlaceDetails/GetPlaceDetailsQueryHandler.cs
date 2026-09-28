using Sportner.Application.Abstractions.Location;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Locations;

namespace Sportner.Application.Features.Locations.GetPlaceDetails;

internal sealed class GetPlaceDetailsQueryHandler
    : IQueryHandler<GetPlaceDetailsQuery, ResolvedPlaceResponse>
{
    private readonly IPlaceProvider _placeProvider;

    public GetPlaceDetailsQueryHandler(IPlaceProvider placeProvider)
    {
        _placeProvider = placeProvider;
    }

    public async Task<Result<ResolvedPlaceResponse>> Handle(
        GetPlaceDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _placeProvider.GetDetailsAsync(
            request.PlaceId,
            request.Language,
            request.SessionToken,
            cancellationToken);

        if (result.IsFailure)
        {
            return Result<ResolvedPlaceResponse>.Failure(LocationErrors.PlaceNotFound);
        }

        var place = result.Value!;

        return Result<ResolvedPlaceResponse>.Success(
            new ResolvedPlaceResponse(place.AddressText, place.Latitude, place.Longitude));
    }
}
