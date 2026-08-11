using Microsoft.Extensions.Logging;
using Sportner.Application.Abstractions.Notifications;

namespace Sportner.Infrastructure.Notifications;

/// <summary>
/// Placeholder push sender until FCM/APNs credentials are configured.
/// Logs delivery (token masked) and reports success so the outbox pipeline can be verified end-to-end.
/// </summary>
public sealed class LoggingPushSender : IPushSender
{
    private readonly ILogger<LoggingPushSender> _logger;

    public LoggingPushSender(ILogger<LoggingPushSender> logger)
    {
        _logger = logger;
    }

    public Task<PushSendResult> SendAsync(
        PushMessage message,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;

        _logger.LogInformation(
            "Push dispatched to user {UserId} device {DeviceId} ({Platform}) token {TokenMask}: {Type} — {Title}",
            message.UserId,
            message.DeviceId,
            message.Platform,
            Mask(message.PushToken),
            message.NotificationType,
            message.Title);

        return Task.FromResult(PushSendResult.Ok());
    }

    private static string Mask(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length <= 8)
        {
            return "****";
        }

        return string.Concat(token.AsSpan(0, 4), "…", token.AsSpan(token.Length - 4));
    }
}
