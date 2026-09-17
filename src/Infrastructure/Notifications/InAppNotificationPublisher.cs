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

        if (actorUserId is { } actor)
        {
            var blocked = await _dbContext.UserBlocks.AsNoTracking()
                .AnyAsync(
                    block =>
                        (block.BlockerUserId == recipientUserId && block.BlockedUserId == actor)
                        || (block.BlockerUserId == actor && block.BlockedUserId == recipientUserId),
                    cancellationToken);

            if (blocked)
            {
                return;
            }
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

        (title, body) = await FormatActorNotificationAsync(
            recipientUserId,
            actorUserId,
            title,
            body,
            cancellationToken);

        Guid? notificationId = null;

        if (deliverInApp)
        {
            Notification? notification = null;

            // Several messages from the same person while still unread fold into one
            // notification (bumped occurrence count + latest preview) instead of piling up as
            // separate rows — mirrors how a chat app's notification tray groups by sender.
            if (type == NotificationType.NewMessage && actorUserId is { } messageActor)
            {
                notification = await _dbContext.Notifications
                    .FirstOrDefaultAsync(
                        candidate =>
                            candidate.RecipientUserId == recipientUserId
                            && candidate.ActorUserId == messageActor
                            && candidate.NotificationType == type
                            && candidate.EntityType == entityType
                            && candidate.EntityId == entityId
                            && !candidate.IsRead,
                        cancellationToken);
            }

            if (notification is not null)
            {
                notification.IncrementOccurrence(title, body, utcNow);
            }
            else
            {
                notification = Notification.Create(
                    recipientUserId,
                    actorUserId,
                    type,
                    entityType,
                    entityId,
                    title,
                    body,
                    utcNow);

                _dbContext.Notifications.Add(notification);
            }

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
                    actorUserId,
                    notificationId,
                    type,
                    entityType,
                    entityId,
                    title,
                    body,
                    utcNow));
        }
    }

    /// <summary>
    /// Actor-driven notifications use the same hierarchy as a direct message:
    /// the sender is the title, and the action is the body. Existing command
    /// handlers can keep supplying localized sentences, while the delivery
    /// boundary prevents "X kullanıcısı ..." from leaking into push/inbox UI.
    /// </summary>
    private async Task<(string Title, string Body)> FormatActorNotificationAsync(
        Guid recipientUserId,
        Guid? actorUserId,
        string title,
        string body,
        CancellationToken cancellationToken)
    {
        if (actorUserId is not { } actor)
        {
            return (title, body);
        }

        var actorUsername = await _dbContext.UserProfiles.AsNoTracking()
            .Where(profile => profile.UserId == actor)
            .Select(profile => profile.Username)
            .FirstOrDefaultAsync(cancellationToken);
        var recipientLanguage = await _dbContext.Users.AsNoTracking()
            .Where(user => user.Id == recipientUserId)
            .Select(user => user.PreferredLanguage)
            .FirstOrDefaultAsync(cancellationToken);
        var displayName = string.IsNullOrWhiteSpace(actorUsername)
            ? recipientLanguage == Language.English ? "Someone" : "Biri"
            : actorUsername;

        // New-message publishers already provide the intended "name / preview"
        // shape. Reformat only legacy actor sentences.
        if (string.Equals(title, displayName, StringComparison.Ordinal))
        {
            return (title, body);
        }

        return (
            displayName,
            string.Equals(body, title, StringComparison.Ordinal)
                ? RemoveActorPrefix(body, actorUsername, recipientLanguage)
                : body);
    }

    private static string RemoveActorPrefix(
        string sentence,
        string? actorUsername,
        Language recipientLanguage)
    {
        var actorPrefix = string.IsNullOrWhiteSpace(actorUsername)
            ? recipientLanguage == Language.English ? "A user" : "Bir kullanıcı"
            : recipientLanguage == Language.English
                ? actorUsername
                : $"{actorUsername} kullanıcısı";

        if (!sentence.StartsWith(actorPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return sentence;
        }

        var action = sentence[actorPrefix.Length..].TrimStart();
        if (action.Length == 0)
        {
            return sentence;
        }

        var culture = recipientLanguage == Language.English
            ? System.Globalization.CultureInfo.GetCultureInfo("en-US")
            : System.Globalization.CultureInfo.GetCultureInfo("tr-TR");
        return char.ToUpper(action[0], culture) + action[1..];
    }
}
