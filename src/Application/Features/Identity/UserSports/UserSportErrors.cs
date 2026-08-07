using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.UserSports;

internal static class UserSportErrors
{
    internal static readonly Error NotAuthenticated = Error.Unauthorized(
        "UserSport.NotAuthenticated",
        "The request is not associated with an authenticated user.");

    internal static readonly Error UserNotFound = Error.NotFound(
        "UserSport.UserNotFound",
        "The user was not found.");

    internal static readonly Error SportNotFound = Error.NotFound(
        "UserSport.SportNotFound",
        "The sport was not found.");

    internal static readonly Error SportInactive = Error.Validation(
        "UserSport.SportInactive",
        "The sport is not currently available.");

    internal static readonly Error AlreadyAdded = Error.Conflict(
        "UserSport.AlreadyAdded",
        "This sport is already associated with the user.");

    internal static readonly Error NotFound = Error.NotFound(
        "UserSport.NotFound",
        "This sport is not associated with the user.");
}
