using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.UserProfiles;

internal static class ProfileErrors
{
    internal static readonly Error NotAuthenticated = Error.Unauthorized(
        "Profile.NotAuthenticated",
        "The request is not associated with an authenticated user.");

    internal static readonly Error NotFound = Error.NotFound(
        "Profile.NotFound",
        "The profile was not found.");

    internal static readonly Error AlreadyExists = Error.Conflict(
        "Profile.AlreadyExists",
        "This user already has a profile.");

    internal static readonly Error UsernameTaken = Error.Conflict(
        "Profile.UsernameTaken",
        "This username is already in use.");

    internal static readonly Error UsernameChangeTooSoon = Error.Conflict(
        "Profile.UsernameChangeTooSoon",
        "The username can only be changed once every 30 days.");

    internal static readonly Error NotPublic = Error.Forbidden(
        "Profile.NotPublic",
        "This profile is private.");

    internal static readonly Error InvalidMedia = Error.Validation(
        "Profile.InvalidMedia",
        "The uploaded file is missing or has an unsupported content type.");
}
