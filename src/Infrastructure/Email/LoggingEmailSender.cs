using Microsoft.Extensions.Logging;
using Sportner.Application.Abstractions.Email;

namespace Sportner.Infrastructure.Email;

/// <summary>
/// Placeholder sender used until a Resend API key is configured. Logs the code (masked email)
/// and reports success so registration/verification can be exercised end to end in dev/test
/// without paying for or wiring up a real email provider.
/// </summary>
public sealed class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task<EmailSendResult> SendVerificationCodeAsync(
        string toEmail,
        string code,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Verification code for {Email}: {Code} (Resend not configured — logging instead of sending).",
            Mask(toEmail),
            code);

        return Task.FromResult(EmailSendResult.Ok());
    }

    public Task<EmailSendResult> SendPasswordResetCodeAsync(
        string toEmail,
        string code,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Password reset code for {Email}: {Code} (Resend not configured — logging instead of sending).",
            Mask(toEmail),
            code);

        return Task.FromResult(EmailSendResult.Ok());
    }

    private static string Mask(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 1)
        {
            return "****";
        }

        return string.Concat(email.AsSpan(0, 2), "***", email.AsSpan(at));
    }
}
