using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;
using Sportner.Domain.Notifications;

namespace Sportner.Domain.UnitTests.Notifications;

public sealed class NotificationDeliveryOutboxTests
{
    [Fact]
    public void MarkFailed_schedules_retry_until_max_attempts()
    {
        var utcNow = DateTimeOffset.Parse("2026-08-10T12:00:00Z");
        var item = NotificationDeliveryOutbox.CreatePush(
            Guid.NewGuid(),
            null,
            NotificationType.NewMessage,
            NotificationEntityType.Conversation,
            Guid.NewGuid(),
            "Title",
            "Body",
            utcNow);

        for (var i = 1; i < NotificationDeliveryOutbox.MaxAttempts; i++)
        {
            item.MarkFailed("boom", utcNow);
            Assert.Equal(NotificationDeliveryStatus.Pending, item.Status);
            Assert.Equal(i, item.AttemptCount);
            Assert.NotNull(item.NextAttemptAt);
        }

        item.MarkFailed("final", utcNow);
        Assert.Equal(NotificationDeliveryStatus.Failed, item.Status);
        Assert.Equal(NotificationDeliveryOutbox.MaxAttempts, item.AttemptCount);
        Assert.Null(item.NextAttemptAt);
    }

    [Fact]
    public void MarkSent_clears_retry_state()
    {
        var utcNow = DateTimeOffset.Parse("2026-08-10T12:00:00Z");
        var item = NotificationDeliveryOutbox.CreatePush(
            Guid.NewGuid(),
            Guid.NewGuid(),
            NotificationType.FriendRequest,
            NotificationEntityType.User,
            Guid.NewGuid(),
            "Friend",
            "New request",
            utcNow);

        item.MarkSent(utcNow);

        Assert.Equal(NotificationDeliveryStatus.Sent, item.Status);
        Assert.Equal(utcNow, item.SentAt);
        Assert.Null(item.NextAttemptAt);
        Assert.Null(item.LastError);
    }

    [Fact]
    public void Create_rejects_empty_recipient()
    {
        Assert.Throws<DomainException>(() =>
            NotificationDeliveryOutbox.CreatePush(
                Guid.Empty,
                null,
                NotificationType.System,
                NotificationEntityType.User,
                null,
                "T",
                "B",
                DateTimeOffset.UtcNow));
    }
}
