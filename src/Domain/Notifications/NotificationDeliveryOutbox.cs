using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Notifications;

/// <summary>
/// Durable delivery queue for push/email. In-app rows live in <see cref="Notification"/>;
/// this outbox is processed by the Notifications worker with retry.
/// </summary>
public class NotificationDeliveryOutbox : AuditableEntity
{
    public const int MaxAttempts = 5;
    private const int MaxTitleLength = 150;
    private const int MaxBodyLength = 1000;
    private const int MaxErrorLength = 1000;

    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(15),
        TimeSpan.FromHours(1),
        TimeSpan.FromHours(6)
    ];

    private NotificationDeliveryOutbox()
    {
    }

    public Guid RecipientUserId { get; private set; }

    public Guid? NotificationId { get; private set; }

    public NotificationDeliveryChannel Channel { get; private set; }

    public NotificationDeliveryStatus Status { get; private set; }

    public NotificationType NotificationType { get; private set; }

    public NotificationEntityType EntityType { get; private set; }

    public Guid? EntityId { get; private set; }

    public string Title { get; private set; } = null!;

    public string Body { get; private set; } = null!;

    public int AttemptCount { get; private set; }

    public DateTimeOffset? NextAttemptAt { get; private set; }

    public DateTimeOffset? SentAt { get; private set; }

    public string? LastError { get; private set; }

    public static NotificationDeliveryOutbox CreatePush(
        Guid recipientUserId,
        Guid? notificationId,
        NotificationType notificationType,
        NotificationEntityType entityType,
        Guid? entityId,
        string title,
        string body,
        DateTimeOffset utcNow) =>
        Create(
            recipientUserId,
            notificationId,
            NotificationDeliveryChannel.Push,
            notificationType,
            entityType,
            entityId,
            title,
            body,
            utcNow);

    public static NotificationDeliveryOutbox Create(
        Guid recipientUserId,
        Guid? notificationId,
        NotificationDeliveryChannel channel,
        NotificationType notificationType,
        NotificationEntityType entityType,
        Guid? entityId,
        string title,
        string body,
        DateTimeOffset utcNow)
    {
        if (recipientUserId == Guid.Empty)
        {
            throw new DomainException("Recipient user id is required.");
        }

        if (notificationId == Guid.Empty)
        {
            throw new DomainException("Notification id cannot be empty.");
        }

        if (!Enum.IsDefined(channel))
        {
            throw new DomainException("Delivery channel is invalid.");
        }

        if (!Enum.IsDefined(notificationType))
        {
            throw new DomainException("Notification type is invalid.");
        }

        if (!Enum.IsDefined(entityType))
        {
            throw new DomainException("Notification entity type is invalid.");
        }

        return new NotificationDeliveryOutbox
        {
            Id = Guid.NewGuid(),
            RecipientUserId = recipientUserId,
            NotificationId = notificationId,
            Channel = channel,
            Status = NotificationDeliveryStatus.Pending,
            NotificationType = notificationType,
            EntityType = entityType,
            EntityId = entityId,
            Title = NormalizeTitle(title),
            Body = NormalizeBody(body),
            AttemptCount = 0,
            NextAttemptAt = utcNow,
            CreatedAt = utcNow
        };
    }

    public void MarkSent(DateTimeOffset utcNow)
    {
        Status = NotificationDeliveryStatus.Sent;
        SentAt = utcNow;
        NextAttemptAt = null;
        LastError = null;
        Touch(utcNow);
    }

    /// <summary>
    /// Soft-skip (e.g. no devices). Does not retry.
    /// </summary>
    public void MarkCancelled(string reason, DateTimeOffset utcNow)
    {
        Status = NotificationDeliveryStatus.Cancelled;
        NextAttemptAt = null;
        LastError = NormalizeError(reason);
        Touch(utcNow);
    }

    public void MarkFailed(string error, DateTimeOffset utcNow)
    {
        AttemptCount++;
        LastError = NormalizeError(error);

        if (AttemptCount >= MaxAttempts)
        {
            Status = NotificationDeliveryStatus.Failed;
            NextAttemptAt = null;
        }
        else
        {
            Status = NotificationDeliveryStatus.Pending;
            var delayIndex = Math.Min(AttemptCount - 1, RetryDelays.Length - 1);
            NextAttemptAt = utcNow.Add(RetryDelays[delayIndex]);
        }

        Touch(utcNow);
    }

    public bool IsDue(DateTimeOffset utcNow) =>
        Status == NotificationDeliveryStatus.Pending
        && (NextAttemptAt is null || NextAttemptAt <= utcNow);

    private void Touch(DateTimeOffset utcNow) => UpdatedAt = utcNow;

    private static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Outbox title is required.");
        }

        var normalized = title.Trim();
        if (normalized.Length > MaxTitleLength)
        {
            throw new DomainException($"Outbox title cannot exceed {MaxTitleLength} characters.");
        }

        return normalized;
    }

    private static string NormalizeBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new DomainException("Outbox body is required.");
        }

        var normalized = body.Trim();
        if (normalized.Length > MaxBodyLength)
        {
            throw new DomainException($"Outbox body cannot exceed {MaxBodyLength} characters.");
        }

        return normalized;
    }

    private static string NormalizeError(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            return "Unknown error.";
        }

        var normalized = error.Trim();
        return normalized.Length <= MaxErrorLength
            ? normalized
            : normalized[..MaxErrorLength];
    }
}
