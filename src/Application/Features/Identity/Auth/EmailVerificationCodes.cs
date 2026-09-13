using System.Security.Cryptography;

namespace Sportner.Application.Features.Identity.Auth;

/// <summary>Shared policy + code generation for email verification, used by Register,
/// CompleteExternalRegistration (Apple hides email on repeat sign-ins, so it isn't guaranteed
/// there either) and the dedicated resend/verify endpoints.</summary>
internal static class EmailVerificationCodes
{
    public static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);

    /// <summary>Cryptographically random 6-digit code, zero-padded (e.g. "004821").</summary>
    public static string Generate() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
}
