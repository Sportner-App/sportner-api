using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Abstractions.Notifications;

public sealed record PushMessage(
    Guid UserId,
    Guid DeviceId,
    DevicePlatform Platform,
    string PushToken,
    string Title,
    string Body,
    NotificationType NotificationType,
    NotificationEntityType EntityType,
    Guid? EntityId);

public sealed record PushSendResult(bool Succeeded, bool InvalidToken, string? ErrorMessage)
{
    public static PushSendResult Ok() => new(true, false, null);

    public static PushSendResult Invalid(string? error = null) =>
        new(false, true, error ?? "Push token is invalid.");

    public static PushSendResult Failed(string error) => new(false, false, error);
}

public interface IPushSender
{
    Task<PushSendResult> SendAsync(PushMessage message, CancellationToken cancellationToken = default);
}
