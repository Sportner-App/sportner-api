using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.SavedLocations;

internal static class SavedLocationErrors
{
    internal static readonly Error NotAuthenticated = Error.Unauthorized(
        "SavedLocation.NotAuthenticated",
        "The request is not associated with an authenticated user.");

    internal static readonly Error UserNotFound = Error.NotFound(
        "SavedLocation.UserNotFound",
        "The user was not found.");

    internal static readonly Error NotFound = Error.NotFound(
        "SavedLocation.NotFound",
        "The saved location was not found.");
}
