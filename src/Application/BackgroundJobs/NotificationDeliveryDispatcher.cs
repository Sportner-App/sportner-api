using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sportner.Application.Abstractions.BackgroundJobs;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Notifications;

namespace Sportner.Application.BackgroundJobs;

internal sealed class NotificationDeliveryDispatcher : INotificationDeliveryDispatcher
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IPushSender _pushSender;
    private readonly TimeProvider _timeProvider;
    private readonly BackgroundJobsOptions _options;
    private readonly ILogger<NotificationDeliveryDispatcher> _logger;

    public NotificationDeliveryDispatcher(
        IApplicationDbContext dbContext,
        IPushSender pushSender,
        TimeProvider timeProvider,
        IOptions<BackgroundJobsOptions> options,
        ILogger<NotificationDeliveryDispatcher> logger)
    {
        _dbContext = dbContext;
        _pushSender = pushSender;
        _timeProvider = timeProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task DispatchPendingAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = _timeProvider.GetUtcNow();
        var batchSize = Math.Max(1, _options.NotificationDeliveryBatchSize);

        var pending = await _dbContext.NotificationDeliveryOutbox
            .Where(item =>
                item.Status == NotificationDeliveryStatus.Pending
                && (item.NextAttemptAt == null || item.NextAttemptAt <= utcNow))
            .OrderBy(item => item.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Processing {Count} notification delivery outbox rows.", pending.Count);

        foreach (var item in pending)
        {
            try
            {
                await ProcessItemAsync(item, utcNow, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected failure processing outbox {OutboxId}.",
                    item.Id);
                item.MarkFailed(ex.Message, _timeProvider.GetUtcNow());
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessItemAsync(
        NotificationDeliveryOutbox item,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (item.Channel != NotificationDeliveryChannel.Push)
        {
            item.MarkCancelled("Email delivery is not enabled yet.", utcNow);
            return;
        }

        var devices = await _dbContext.UserDevices
            .Where(device =>
                device.UserId == item.RecipientUserId
                && device.PushToken != null
                && device.PushToken != "")
            .ToListAsync(cancellationToken);

        if (devices.Count == 0)
        {
            item.MarkCancelled("No devices with a push token.", utcNow);
            return;
        }

        var anySuccess = false;
        string? lastError = null;

        foreach (var device in devices)
        {
            var result = await _pushSender.SendAsync(
                new PushMessage(
                    item.RecipientUserId,
                    device.Id,
                    device.Platform,
                    device.PushToken!,
                    item.Title,
                    item.Body,
                    item.NotificationType,
                    item.EntityType,
                    item.EntityId),
                cancellationToken);

            if (result.Succeeded)
            {
                anySuccess = true;
                continue;
            }

            lastError = result.ErrorMessage;

            if (result.InvalidToken)
            {
                device.ClearPushToken(utcNow);
                _logger.LogInformation(
                    "Cleared invalid push token for device {DeviceId}, user {UserId}.",
                    device.Id,
                    item.RecipientUserId);
            }
        }

        if (anySuccess)
        {
            item.MarkSent(utcNow);
            return;
        }

        item.MarkFailed(lastError ?? "Push send failed for all devices.", utcNow);
    }
}
