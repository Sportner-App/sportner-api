using Sportner.Application.Common.Results;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Notifications;

/// <summary>
/// Her hata mesajı <see cref="ErrorMessagesResource"/> üzerinden çözülür.
/// </summary>
internal static class NotificationErrors
{
    internal static Error NotAuthenticated => Error.Unauthorized(
        "Notification.NotAuthenticated",
        ErrorMessagesResource.Notification_NotAuthenticated);

    internal static Error NotFound => Error.NotFound(
        "Notification.NotFound",
        ErrorMessagesResource.Notification_NotFound);

    internal static Error SettingNotFound => Error.NotFound(
        "Notification.SettingNotFound",
        ErrorMessagesResource.Notification_SettingNotFound);

    internal static Error InvalidType => Error.Validation(
        "Notification.InvalidType",
        ErrorMessagesResource.Notification_InvalidType);

    internal static Error InvalidCursor => Error.Validation(
        "Notification.InvalidCursor",
        ErrorMessagesResource.Notification_InvalidCursor);
}

public sealed record NotificationResponse(
    Guid Id,
    short NotificationType,
    short EntityType,
    Guid? EntityId,
    Guid? ActorUserId,
    string? ActorUsername,
    string Title,
    string Body,
    bool IsRead,
    DateTimeOffset? ReadAt,
    DateTimeOffset CreatedAt);

public sealed record NotificationSettingResponse(
    short NotificationType,
    bool InAppEnabled,
    bool PushEnabled,
    bool EmailEnabled);
