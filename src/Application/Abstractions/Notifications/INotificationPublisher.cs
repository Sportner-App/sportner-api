using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Abstractions.Notifications;

/// <summary>
/// Creates in-app notifications for other modules. Push/email delivery is deferred to jobs.
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
