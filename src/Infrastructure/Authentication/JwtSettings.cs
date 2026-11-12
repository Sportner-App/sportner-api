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
    /// Erişim anahtarının ömrü (dakika). Çıkış ve oturum iptali erişim anahtarını geçersiz
    /// kılmadığı için kısa tutulur; ama yayındaki uygulama (build 52) yenileme isteği ağda
    /// koparsa kullanıcıyı çıkış yaptırıyor. Uygulama düzeltmesi yayılana kadar 24 saat.
    /// </summary>
    public int ExpirationMinutes { get; set; } = 60 * 24;

    /// <summary>
    /// Refresh token lifetime in days.
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 90;
}
