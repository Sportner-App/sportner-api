using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.Auth;

internal static class AuthErrors
{
    internal static readonly Error InvalidCredentials = Error.Unauthorized(
        "Auth.InvalidCredentials",
        "Invalid username or password.");

    internal static readonly Error UsernameTaken = Error.Conflict(
        "Auth.UsernameTaken",
        "This username is already taken.");

    internal static readonly Error AccountNotAccessible = Error.Forbidden(
        "Auth.AccountNotAccessible",
        "This account cannot sign in.");

    internal static readonly Error InvalidRefreshToken = Error.Unauthorized(
        "Auth.InvalidRefreshToken",
        "The refresh token is invalid, expired or revoked.");
}
