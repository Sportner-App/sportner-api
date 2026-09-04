namespace Sportner.Infrastructure.Authentication;

/// <summary>
/// Bound from the <c>AppleAuth</c> configuration section.
/// </summary>
public sealed class AppleAuthOptions
{
    public const string SectionName = "AppleAuth";

    /// <summary>The app's bundle id — native Sign in with Apple issues identity tokens with this as the audience.</summary>
    public string BundleId { get; set; } = string.Empty;

    public string Issuer { get; set; } = "https://appleid.apple.com";
}
