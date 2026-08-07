using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Gamification;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Domain.Badges;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Gamification;

internal sealed class BadgeAwarder : IBadgeAwarder
{
    private readonly IApplicationDbContext _dbContext;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly TimeProvider _timeProvider;

    public BadgeAwarder(
        IApplicationDbContext dbContext,
        INotificationPublisher notificationPublisher,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _notificationPublisher = notificationPublisher;
        _timeProvider = timeProvider;
    }

    public async Task TryAwardAsync(
        Guid userId,
        string badgeCode,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(badgeCode))
        {
            return;
        }

        var badge = await _dbContext.Badges
            .FirstOrDefaultAsync(
                candidate => candidate.Code == badgeCode && candidate.IsActive,
                cancellationToken);

        if (badge is null || !badge.IsEarnable())
        {
            return;
        }

        var alreadyAwarded = await _dbContext.UserBadges.AsNoTracking()
            .AnyAsync(
                userBadge => userBadge.UserId == userId && userBadge.BadgeId == badge.Id,
                cancellationToken);

        if (alreadyAwarded)
        {
            return;
        }

        var utcNow = _timeProvider.GetUtcNow();
        _dbContext.UserBadges.Add(UserBadge.Award(userId, badge.Id, utcNow, utcNow));

        var statistics = await _dbContext.UserStatistics
            .FirstOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken);

        statistics?.IncreaseBadgesCount(utcNow);

        await _notificationPublisher.PublishAsync(
            userId,
            NotificationType.BadgeEarned,
            "Rozet kazandın",
            $"'{badge.Name}' rozetini kazandın.",
            NotificationEntityType.Badge,
            badge.Id,
            actorUserId: null,
            cancellationToken);
    }
}
