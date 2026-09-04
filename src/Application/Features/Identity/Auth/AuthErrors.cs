using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.Auth;

internal static class AuthErrors
{
    internal static readonly Error InvalidCredentials = Error.Unauthorized(
        "Auth.InvalidCredentials",
        "Kullanıcı adı veya şifre hatalı.");

    internal static readonly Error UsernameTaken = Error.Conflict(
        "Auth.UsernameTaken",
        "Bu kullanıcı adı zaten alınmış.");

    internal static readonly Error AccountNotAccessible = Error.Forbidden(
        "Auth.AccountNotAccessible",
        "Bu hesapla giriş yapılamıyor.");

    internal static readonly Error InvalidRefreshToken = Error.Unauthorized(
        "Auth.InvalidRefreshToken",
        "Oturum yenileme bilgisi geçersiz, süresi dolmuş veya iptal edilmiş.");

    internal static readonly Error ExternalTokenInvalid = Error.Unauthorized(
        "Auth.ExternalTokenInvalid",
        "Kimlik doğrulama sağlayıcısından gelen bilgi doğrulanamadı.");

    internal static readonly Error ExternalRegistrationTokenInvalid = Error.Unauthorized(
        "Auth.ExternalRegistrationTokenInvalid",
        "Sosyal kayıt oturumunun süresi dolmuş veya bilgi geçersiz.");

    internal static readonly Error ExternalLoginAlreadyRegistered = Error.Conflict(
        "Auth.ExternalLoginAlreadyRegistered",
        "Bu sosyal hesap zaten kayıtlı. Giriş yapmayı tekrar dene.");
}
