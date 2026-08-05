using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Notifications;

public class NotificationSetting : AggregateRoot
{
    private NotificationSetting()
    {
    }

    public Guid UserId { get; private set; }

    public NotificationType NotificationType { get; private set; }

    public bool InAppEnabled { get; private set; }

    public bool PushEnabled { get; private set; }

    public bool EmailEnabled { get; private set; }

    public static NotificationSetting Create(
        Guid userId,
        NotificationType notificationType,
        bool inAppEnabled,
        bool pushEnabled,
        bool emailEnabled,
        DateTimeOffset utcNow)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        EnsureDefinedNotificationType(notificationType);

        return new NotificationSetting
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            NotificationType = notificationType,
            InAppEnabled = inAppEnabled,
            PushEnabled = pushEnabled,
            EmailEnabled = emailEnabled,
            CreatedAt = utcNow
        };
    }

    public static NotificationSetting CreateDefault(
        Guid userId,
        NotificationType notificationType,
        DateTimeOffset utcNow)
    {
        var (inAppEnabled, pushEnabled, emailEnabled) = ResolveDefaults(notificationType);

        return Create(
            userId,
            notificationType,
            inAppEnabled,
            pushEnabled,
            emailEnabled,
            utcNow);
    }

    public void EnableInApp(DateTimeOffset utcNow)
    {
        if (InAppEnabled)
        {
            return;
        }

        InAppEnabled = true;
        Touch(utcNow);
    }

    public void DisableInApp(DateTimeOffset utcNow)
    {
        if (!InAppEnabled)
        {
            return;
        }

        InAppEnabled = false;
        Touch(utcNow);
    }

    public void EnablePush(DateTimeOffset utcNow)
    {
        if (PushEnabled)
        {
            return;
        }

        PushEnabled = true;
        Touch(utcNow);
    }

    public void DisablePush(DateTimeOffset utcNow)
    {
        if (!PushEnabled)
        {
            return;
        }

        PushEnabled = false;
        Touch(utcNow);
    }

    public void EnableEmail(DateTimeOffset utcNow)
    {
        if (EmailEnabled)
        {
            return;
        }

        EmailEnabled = true;
        Touch(utcNow);
    }

    public void DisableEmail(DateTimeOffset utcNow)
    {
        if (!EmailEnabled)
        {
            return;
        }

        EmailEnabled = false;
        Touch(utcNow);
    }

    public void UpdateChannels(
        bool inAppEnabled,
        bool pushEnabled,
        bool emailEnabled,
        DateTimeOffset utcNow)
    {
        if (InAppEnabled == inAppEnabled
            && PushEnabled == pushEnabled
            && EmailEnabled == emailEnabled)
        {
            return;
        }

        InAppEnabled = inAppEnabled;
        PushEnabled = pushEnabled;
        EmailEnabled = emailEnabled;
        Touch(utcNow);
    }

    public bool CanDeliverInApp()
    {
        return InAppEnabled;
    }

    public bool CanDeliverPush()
    {
        return PushEnabled;
    }

    public bool CanDeliverEmail()
    {
        return EmailEnabled;
    }

    private void Touch(DateTimeOffset utcNow)
    {
        UpdatedAt = utcNow;
    }

    private static (bool InAppEnabled, bool PushEnabled, bool EmailEnabled) ResolveDefaults(
        NotificationType notificationType)
    {
        EnsureDefinedNotificationType(notificationType);

        return notificationType switch
        {
            NotificationType.FriendRequest => (true, true, false),
            NotificationType.FriendAccepted => (true, true, false),
            NotificationType.EventInvitation => (true, true, false),
            NotificationType.EventRequestApproved => (true, true, false),
            NotificationType.EventRequestRejected => (true, true, false),
            NotificationType.EventReminder => (true, true, false),
            NotificationType.EventCancelled => (true, true, false),
            NotificationType.PostLiked => (true, false, false),
            NotificationType.PostCommented => (true, true, false),
            NotificationType.CommentReplied => (true, true, false),
            NotificationType.BadgeEarned => (true, true, false),
            NotificationType.NewMessage => (true, true, false),
            NotificationType.System => (true, true, true),
            _ => throw new DomainException("Notification type is unsupported.")
        };
    }

    private static void EnsureDefinedNotificationType(NotificationType notificationType)
    {
        if (!Enum.IsDefined(notificationType))
        {
            throw new DomainException("Notification type is invalid.");
        }
    }
}
