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
    /// Erişim anahtarının ömrü (dakika). Kısa tutulur: çıkış ve oturum iptali erişim anahtarını
    /// geçersiz kılmadığı için çalınan bir anahtar en fazla bu kadar işe yarar. İstemciler 401'de
    /// yenileme anahtarıyla yenisini alır.
    /// </summary>
    public int ExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Refresh token lifetime in days.
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 90;
}
