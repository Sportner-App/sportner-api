using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Notifications;

namespace Sportner.Infrastructure.Notifications;

/// <summary>
/// Persists in-app notifications and enqueues push delivery when settings allow.
/// Does not call <c>SaveChanges</c> — the caller owns the unit of work.
/// Email channel is deferred (settings respected later when <c>IEmailSender</c> lands).
/// </summary>
public sealed class InAppNotificationPublisher : INotificationPublisher
{
    private readonly IApplicationDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<InAppNotificationPublisher> _logger;

    public InAppNotificationPublisher(
        IApplicationDbContext dbContext,
        TimeProvider timeProvider,
        ILogger<InAppNotificationPublisher> logger)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task PublishAsync(
        Guid recipientUserId,
        NotificationType type,
        string title,
        string body,
        NotificationEntityType entityType,
        Guid? entityId,
        Guid? actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId is not null && actorUserId == recipientUserId)
        {
            return;
        }

        var utcNow = _timeProvider.GetUtcNow();

        var setting = await _dbContext.NotificationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                candidate =>
                    candidate.UserId == recipientUserId
                    && candidate.NotificationType == type,
                cancellationToken);

        // Missing row → type defaults (same as CreateDefault) without inserting.
        var effective = setting
            ?? NotificationSetting.CreateDefault(recipientUserId, type, utcNow);

        var deliverInApp = effective.CanDeliverInApp();
        var deliverPush = effective.CanDeliverPush();

        if (!deliverInApp && !deliverPush)
        {
            _logger.LogDebug(
                "Skipping notification {Type} for user {UserId}: all channels disabled.",
                type,
                recipientUserId);
            return;
        }

        Guid? notificationId = null;

        if (deliverInApp)
        {
            var notification = Notification.Create(
                recipientUserId,
                actorUserId,
                type,
                entityType,
                entityId,
                title,
                body,
                utcNow);

            _dbContext.Notifications.Add(notification);
            notificationId = notification.Id;
        }
        else
        {
            _logger.LogDebug(
                "Skipping in-app notification {Type} for user {UserId}: channel disabled.",
                type,
                recipientUserId);
        }

        if (deliverPush)
        {
            _dbContext.NotificationDeliveryOutbox.Add(
                NotificationDeliveryOutbox.CreatePush(
                    recipientUserId,
                    notificationId,
                    type,
                    entityType,
                    entityId,
                    title,
                    body,
                    utcNow));
        }
    }
}
