using Sportner.Application.Common.Results;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Identity.Auth;

/// <summary>
/// Her hata mesajı <see cref="ErrorMessagesResource"/> üzerinden çözülür.
/// </summary>
internal static class AuthErrors
{
    internal static Error InvalidCredentials => Error.Unauthorized(
        "Auth.InvalidCredentials",
        ErrorMessagesResource.Auth_InvalidCredentials);

    internal static Error UsernameTaken => Error.Conflict(
        "Auth.UsernameTaken",
        ErrorMessagesResource.Auth_UsernameTaken);

    internal static Error AccountNotAccessible => Error.Forbidden(
        "Auth.AccountNotAccessible",
        ErrorMessagesResource.Auth_AccountNotAccessible);

    internal static Error InvalidRefreshToken => Error.Unauthorized(
        "Auth.InvalidRefreshToken",
        ErrorMessagesResource.Auth_InvalidRefreshToken);

    internal static Error ExternalTokenInvalid => Error.Unauthorized(
        "Auth.ExternalTokenInvalid",
        ErrorMessagesResource.Auth_ExternalTokenInvalid);

    internal static Error ExternalRegistrationTokenInvalid => Error.Unauthorized(
        "Auth.ExternalRegistrationTokenInvalid",
        ErrorMessagesResource.Auth_ExternalRegistrationTokenInvalid);

    internal static Error ExternalLoginAlreadyRegistered => Error.Conflict(
        "Auth.ExternalLoginAlreadyRegistered",
        ErrorMessagesResource.Auth_ExternalLoginAlreadyRegistered);
}
