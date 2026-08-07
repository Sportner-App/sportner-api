using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.Sessions;

internal static class SessionErrors
{
    internal static readonly Error NotAuthenticated = Error.Unauthorized(
        "Session.NotAuthenticated",
        "The request is not associated with an authenticated user.");

    internal static readonly Error UserNotFound = Error.NotFound(
        "Session.UserNotFound",
        "The user was not found.");

    internal static readonly Error NotFound = Error.NotFound(
        "Session.NotFound",
        "The session was not found.");
}
