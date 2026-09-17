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

    internal static Error EmailTaken => Error.Conflict(
        "Auth.EmailTaken",
        ErrorMessagesResource.Auth_EmailTaken);

    internal static Error EmailRequired => Error.Validation(
        "Auth.EmailRequired",
        ErrorMessagesResource.Auth_EmailRequired);

    internal static Error EmailAlreadyVerified => Error.Conflict(
        "Auth.EmailAlreadyVerified",
        ErrorMessagesResource.Auth_EmailAlreadyVerified);

    internal static Error EmailVerificationCodeInvalid => Error.Unauthorized(
        "Auth.EmailVerificationCodeInvalid",
        ErrorMessagesResource.Auth_EmailVerificationCodeInvalid);

    internal static Error EmailVerificationCodeExpired => Error.Unauthorized(
        "Auth.EmailVerificationCodeExpired",
        ErrorMessagesResource.Auth_EmailVerificationCodeExpired);

    internal static Error EmailVerificationCooldown => Error.Conflict(
        "Auth.EmailVerificationCooldown",
        ErrorMessagesResource.Auth_EmailVerificationCooldown);

    /// <summary>
    /// Deliberately generic — covers "code wrong", "code expired" and "no such account" alike so
    /// the reset endpoint never reveals which case applies (account enumeration defense).
    /// </summary>
    internal static Error PasswordResetCodeInvalid => Error.Unauthorized(
        "Auth.PasswordResetCodeInvalid",
        ErrorMessagesResource.Auth_PasswordResetCodeInvalid);

    internal static Error PasswordResetCooldown => Error.Conflict(
        "Auth.PasswordResetCooldown",
        ErrorMessagesResource.Auth_PasswordResetCooldown);
}
