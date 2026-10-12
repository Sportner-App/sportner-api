namespace Sportner.Application.Features.Identity.Auth;

/// <summary>Shared policy + code generation for password reset, used by ForgotPassword and
/// ResetPassword. Code generation is delegated to <see cref="EmailVerificationCodes"/>.</summary>
internal static class PasswordResetCodes
{
    public static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);

    public static string Generate() => EmailVerificationCodes.Generate();
}
