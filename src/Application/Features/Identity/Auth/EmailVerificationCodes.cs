using System.Linq;
using System.Security.Cryptography;

namespace Sportner.Application.Features.Identity.Auth;

/// <summary>Shared policy + code generation for email verification, used by Register,
/// CompleteExternalRegistration (Apple hides email on repeat sign-ins, so it isn't guaranteed
/// there either) and the dedicated resend/verify endpoints.</summary>
internal static class EmailVerificationCodes
{
    public static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Cryptographically random 6-digit code, zero-padded (e.g. "004821") — unless
    /// EmailVerification__StaticCode is set (friend/QA testing without a verified email
    /// domain), in which case every issued code is that fixed value. Remove the env var
    /// once real delivery is set up; this must never be set outside a test environment.
    /// </summary>
    public static string Generate()
    {
        var staticCode = Environment.GetEnvironmentVariable("EmailVerification__StaticCode");
        if (!string.IsNullOrWhiteSpace(staticCode) && staticCode.Length == 6 && staticCode.All(char.IsAsciiDigit))
        {
            return staticCode;
        }

        return RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
    }
}
