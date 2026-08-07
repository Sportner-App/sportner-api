namespace Sportner.Infrastructure.Authentication;

/// <summary>
/// Bound from the optional <c>Otp</c> configuration section.
/// </summary>
public sealed class OtpOptions
{
    public const string SectionName = "Otp";

    public int CodeLength { get; set; } = 6;

    public int ExpirationMinutes { get; set; } = 5;

    /// <summary>
    /// When true, the OTP may be written to logs and <see cref="FixedCode"/> is honored.
    /// Temporary until a real SMS provider is wired — turn off for real production hardening.
    /// </summary>
    public bool ExposeCodeInLogs { get; set; }

    /// <summary>
    /// Optional fixed OTP for UI/API testing (e.g. <c>000000</c>).
    /// Only used when <see cref="ExposeCodeInLogs"/> is true.
    /// </summary>
    public string? FixedCode { get; set; }
}
