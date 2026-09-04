namespace Sportner.Infrastructure.Authentication;

/// <summary>
/// Bound from the <c>GoogleAuth</c> configuration section. <see cref="WebClientId"/> is the
/// audience Google mints mobile ID tokens for when the client configures a webClientId
/// (the recommended pattern for verifying tokens server-side) — not a secret.
/// </summary>
public sealed class GoogleAuthOptions
{
    public const string SectionName = "GoogleAuth";

    public string WebClientId { get; set; } = string.Empty;
}
