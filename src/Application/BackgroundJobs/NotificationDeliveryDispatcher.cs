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
    /// <summary>
    /// The API's inline delivery service and the dedicated Notifications worker both poll
    /// this outbox concurrently. A row claimed for delivery but never resolved (process
    /// crash/restart mid-send) is treated as abandoned and re-claimable after this long.
    /// </summary>
    private static readonly TimeSpan StaleClaimTimeout = TimeSpan.FromMinutes(2);

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
        var staleBefore = utcNow - StaleClaimTimeout;

        // Atomically claim a batch so a concurrently-running dispatcher (the API's inline
        // delivery service and the dedicated Notifications worker, or multiple replicas of
        // either) cannot pick up the same rows and send the same push twice.
        var claimedIds = await _dbContext.ClaimNotificationDeliveryOutboxAsync(
            batchSize,
            utcNow,
            staleBefore,
            cancellationToken);

        if (claimedIds.Count == 0)
        {
            return;
        }

        var pending = await _dbContext.NotificationDeliveryOutbox
            .Where(item => claimedIds.Contains(item.Id))
            .ToListAsync(cancellationToken);

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

        // A device re-registers with a fresh DeviceIdentifier whenever its local storage is
        // reset (reinstall, storage clear), leaving stale rows that still carry the same, still
        // valid push token as the current one. Sending to every row would double (or more) the
        // push for that single physical device, so keep only the most recently touched row per
        // distinct token.
        var devices = await _dbContext.UserDevices
            .Where(device =>
                device.UserId == item.RecipientUserId
                && device.PushToken != null
                && device.PushToken != "")
            .OrderByDescending(device => device.UpdatedAt ?? device.CreatedAt)
            .ToListAsync(cancellationToken);

        devices = devices
            .GroupBy(device => device.PushToken)
            .Select(group => group.First())
            .ToList();

        if (devices.Count == 0)
        {
            item.MarkCancelled("No devices with a push token.", utcNow);
            return;
        }

        // Actor-driven notifications (someone commented, liked, invited...) show that person's
        // photo as the OS notification icon, WhatsApp-style, instead of the generic app logo.
        string? actorAvatarUrl = item.ActorUserId is { } actorId
            ? await _dbContext.UserProfiles.AsNoTracking()
                .Where(profile => profile.UserId == actorId)
                .Select(profile => profile.ProfileImageUrl)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var anySuccess = false;
        string? lastError = null;

        foreach (var device in devices)
        {
            var result = await _pushSender.SendAsync(
                new PushMessage(
                    item.NotificationId,
                    item.RecipientUserId,
                    device.Id,
                    device.Platform,
                    device.PushToken!,
                    item.Title,
                    item.Body,
                    item.NotificationType,
                    item.EntityType,
                    item.EntityId,
                    actorAvatarUrl),
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
