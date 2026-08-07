using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Notifications;

namespace Sportner.Infrastructure.Notifications;

/// <summary>
/// Persists in-app notifications when the recipient's settings allow it.
/// Skips self-notifications. Push/email are intentionally not dispatched here.
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

        var setting = await _dbContext.NotificationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                candidate =>
                    candidate.UserId == recipientUserId
                    && candidate.NotificationType == type,
                cancellationToken);

        if (setting is not null && !setting.CanDeliverInApp())
        {
            _logger.LogDebug(
                "Skipping in-app notification {Type} for user {UserId}: channel disabled.",
                type,
                recipientUserId);
            return;
        }

        // Missing settings default to deliver — CreateDefault at signup should cover this.
        _dbContext.Notifications.Add(Notification.Create(
            recipientUserId,
            actorUserId,
            type,
            entityType,
            entityId,
            title,
            body,
            _timeProvider.GetUtcNow()));
    }
}
