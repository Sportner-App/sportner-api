using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Abstractions.Notifications;

/// <summary>
/// Creates in-app notifications and enqueues push/email delivery for other modules.
/// Does not call <c>SaveChanges</c> — the caller owns the unit of work.
/// </summary>
public interface INotificationPublisher
{
    Task PublishAsync(
        Guid recipientUserId,
        NotificationType type,
        string title,
        string body,
        NotificationEntityType entityType,
        Guid? entityId,
        Guid? actorUserId,
        CancellationToken cancellationToken = default);
}
