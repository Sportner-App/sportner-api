using Microsoft.Extensions.Logging;
using Sportner.Application.Abstractions.Authentication;

namespace Sportner.Infrastructure.Authentication;

/// <summary>
/// Placeholder SMS sender used until a real provider is wired in. It logs only that a message
/// was dispatched to a masked number — never the message body (which may contain the OTP).
/// </summary>
public sealed class LoggingSmsSender : ISmsSender
{
    private readonly ILogger<LoggingSmsSender> _logger;

    public LoggingSmsSender(ILogger<LoggingSmsSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(
        string phoneNumber,
        string message,
        CancellationToken cancellationToken = default)
    {
        _ = message;

        _logger.LogInformation("SMS dispatched to {PhoneNumber}.", Mask(phoneNumber));
        return Task.CompletedTask;
    }

    private static string Mask(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length <= 4)
        {
            return "****";
        }

        return string.Concat("****", phoneNumber.AsSpan(phoneNumber.Length - 4));
    }
}
