namespace Sportner.Infrastructure.Email;

/// <summary>
/// Bound from the <c>Resend</c> configuration section. When <see cref="ApiKey"/> is empty,
/// dependency injection falls back to <see cref="LoggingEmailSender"/> instead — lets the app
/// run (and email flows be exercised end to end) before a Resend account is set up.
/// </summary>
public sealed class ResendOptions
{
    public const string SectionName = "Resend";

    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Must be a domain verified in the Resend dashboard.</summary>
    public string FromAddress { get; set; } = "Sportner <no-reply@sportner.app>";
}
