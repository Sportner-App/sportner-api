namespace Sportner.Infrastructure.Authentication;

/// <summary>
/// Bound from the <c>JwtSettings</c> configuration section.
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "JwtSettings";

    public string Secret { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Access token lifetime in days. Refresh tokens (persisted on sessions) outlive this.
    /// </summary>
    public int ExpirationDays { get; set; } = 7;

    /// <summary>
    /// Refresh token lifetime in days.
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 90;
}
