using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Notifications;

public class Notification : AggregateRoot
{
    private const int MaxTitleLength = 150;
    private const int MaxBodyLength = 1000;

    private Notification()
    {
    }

    public Guid RecipientUserId { get; private set; }

    public Guid? ActorUserId { get; private set; }

    public NotificationType NotificationType { get; private set; }

    public NotificationEntityType EntityType { get; private set; }

    public Guid? EntityId { get; private set; }

    public string Title { get; private set; } = null!;

    public string Body { get; private set; } = null!;

    public bool IsRead { get; private set; }

    public DateTimeOffset? ReadAt { get; private set; }

    public static Notification Create(
        Guid recipientUserId,
        Guid? actorUserId,
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

        if (actorUserId == Guid.Empty)
        {
            throw new DomainException("Actor user id cannot be empty.");
        }

        if (!Enum.IsDefined(notificationType))
        {
            throw new DomainException("Notification type is invalid.");
        }

        if (!Enum.IsDefined(entityType))
        {
            throw new DomainException("Notification entity type is invalid.");
        }

        return new Notification
        {
            Id = Guid.NewGuid(),
            RecipientUserId = recipientUserId,
            ActorUserId = actorUserId,
            NotificationType = notificationType,
            EntityType = entityType,
            EntityId = entityId,
            Title = NormalizeTitle(title),
            Body = NormalizeBody(body),
            IsRead = false,
            ReadAt = null,
            CreatedAt = utcNow
        };
    }

    public void MarkAsRead(DateTimeOffset utcNow)
    {
        if (IsRead)
        {
            return;
        }

        IsRead = true;
        ReadAt = utcNow;
        Touch(utcNow);
    }

    public void MarkAsUnread(DateTimeOffset utcNow)
    {
        if (!IsRead)
        {
            return;
        }

        IsRead = false;
        ReadAt = null;
        Touch(utcNow);
    }

    public bool IsUnread()
    {
        return !IsRead;
    }

    public bool ReferencesEntity(NotificationEntityType entityType, Guid entityId)
    {
        if (!Enum.IsDefined(entityType))
        {
            throw new DomainException("Notification entity type is invalid.");
        }

        if (entityId == Guid.Empty)
        {
            throw new DomainException("Entity id is required.");
        }

        return EntityType == entityType && EntityId == entityId;
    }

    public bool WasTriggeredBy(Guid userId)
    {
        return ActorUserId == userId;
    }

    private void Touch(DateTimeOffset utcNow)
    {
        UpdatedAt = utcNow;
    }

    private static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Notification title is required.");
        }

        var normalized = title.Trim();

        if (normalized.Length > MaxTitleLength)
        {
            throw new DomainException($"Notification title cannot exceed {MaxTitleLength} characters.");
        }

        return normalized;
    }

    private static string NormalizeBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new DomainException("Notification body is required.");
        }

        var normalized = body.Trim();

        if (normalized.Length > MaxBodyLength)
        {
            throw new DomainException($"Notification body cannot exceed {MaxBodyLength} characters.");
        }

        return normalized;
    }
}
