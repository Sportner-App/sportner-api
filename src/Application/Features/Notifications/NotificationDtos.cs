using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Notifications;

internal static class NotificationErrors
{
    internal static readonly Error NotAuthenticated = Error.Unauthorized(
        "Notification.NotAuthenticated",
        "The request is not associated with an authenticated user.");

    internal static readonly Error NotFound = Error.NotFound(
        "Notification.NotFound",
        "The notification was not found.");

    internal static readonly Error SettingNotFound = Error.NotFound(
        "Notification.SettingNotFound",
        "The notification setting was not found.");

    internal static readonly Error InvalidType = Error.Validation(
        "Notification.InvalidType",
        "The notification type is invalid.");

    internal static readonly Error InvalidCursor = Error.Validation(
        "Notification.InvalidCursor",
        "The pagination cursor is invalid.");
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
