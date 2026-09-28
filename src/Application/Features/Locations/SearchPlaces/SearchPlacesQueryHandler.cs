using Sportner.Application.Abstractions.Location;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Locations;

namespace Sportner.Application.Features.Locations.SearchPlaces;

internal sealed class SearchPlacesQueryHandler
    : IQueryHandler<SearchPlacesQuery, IReadOnlyList<PlaceSuggestionResponse>>
{
    /// <summary>Below this the suggestions are noise and every keystroke costs a request.</summary>
    private const int MinimumQueryLength = 2;

    private readonly IPlaceProvider _placeProvider;

    public SearchPlacesQueryHandler(IPlaceProvider placeProvider)
    {
        _placeProvider = placeProvider;
    }

    public async Task<Result<IReadOnlyList<PlaceSuggestionResponse>>> Handle(
        SearchPlacesQuery request,
        CancellationToken cancellationToken)
    {
        var query = request.Query.Trim();

        if (query.Length < MinimumQueryLength)
        {
            return Result<IReadOnlyList<PlaceSuggestionResponse>>.Success([]);
        }

        var result = await _placeProvider.SearchAsync(
            query,
            request.Language,
            request.SessionToken,
            cancellationToken);

        if (result.IsFailure)
        {
            return Result<IReadOnlyList<PlaceSuggestionResponse>>.Failure(LocationErrors.SearchFailed);
        }

        var suggestions = result.Value!
            .Select(suggestion => new PlaceSuggestionResponse(
                suggestion.Id,
                suggestion.PlaceId,
                suggestion.Title,
                suggestion.Subtitle,
                suggestion.AddressText,
                suggestion.Latitude,
                suggestion.Longitude))
            .ToList();

        return Result<IReadOnlyList<PlaceSuggestionResponse>>.Success(suggestions);
    }
}
