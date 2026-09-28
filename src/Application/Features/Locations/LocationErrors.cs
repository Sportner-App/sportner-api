using Sportner.Application.Common.Results;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Locations;

internal static class LocationErrors
{
    internal static Error SearchFailed => Error.Failure(
        "Location.SearchFailed",
        ErrorMessagesResource.Location_SearchFailed);

    internal static Error PlaceNotFound => Error.NotFound(
        "Location.PlaceNotFound",
        ErrorMessagesResource.Location_PlaceNotFound);

    internal static Error ReverseFailed => Error.NotFound(
        "Location.ReverseFailed",
        ErrorMessagesResource.Location_ReverseFailed);
}
