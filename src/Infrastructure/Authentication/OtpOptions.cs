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
    /// When true (development), the generated code is written to the debug log so a tester
    /// can complete the flow without a real SMS provider. Never enable in production.
    /// </summary>
    public bool ExposeCodeInLogs { get; set; }
}
