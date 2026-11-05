using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Abstractions.Notifications;

public sealed record PushMessage(
    Guid? NotificationId,
    Guid UserId,
    Guid DeviceId,
    DevicePlatform Platform,
    string PushToken,
    string Title,
    string Body,
    NotificationType NotificationType,
    NotificationEntityType EntityType,
    Guid? EntityId,
    string? ActorAvatarUrl = null);

public sealed record PushSendResult(
    bool Succeeded,
    bool InvalidToken,
    string? ErrorMessage,
    string? TicketId = null)
{
    public static PushSendResult Ok(string? ticketId = null) => new(true, false, null, ticketId);

    public static PushSendResult Invalid(string? error = null) =>
        new(false, true, error ?? "Push token is invalid.");

    public static PushSendResult Failed(string error) => new(false, false, error);
}

public sealed record PushReceipt(
    bool Delivered,
    bool InvalidToken,
    string? ErrorCode,
    string? ErrorMessage);

public interface IPushSender
{
    Task<PushSendResult> SendAsync(PushMessage message, CancellationToken cancellationToken = default);

    /// <summary>Receipts by ticket id; a ticket with no receipt yet is simply absent.</summary>
    Task<IReadOnlyDictionary<string, PushReceipt>> GetReceiptsAsync(
        IReadOnlyCollection<string> ticketIds,
        CancellationToken cancellationToken = default);
}
