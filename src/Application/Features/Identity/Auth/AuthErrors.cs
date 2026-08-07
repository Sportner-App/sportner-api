using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.Auth;

internal static class AuthErrors
{
    internal static readonly Error InvalidOtp = Error.Unauthorized(
        "Auth.InvalidOtp",
        "The verification code is invalid or has expired.");

    internal static readonly Error AccountNotAccessible = Error.Forbidden(
        "Auth.AccountNotAccessible",
        "This account cannot sign in.");

    internal static readonly Error InvalidRefreshToken = Error.Unauthorized(
        "Auth.InvalidRefreshToken",
        "The refresh token is invalid, expired or revoked.");
}
