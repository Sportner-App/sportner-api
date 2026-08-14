using Sportner.Application.Abstractions.Gamification;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Quests;
using Microsoft.EntityFrameworkCore;

namespace Sportner.Application.Features.Quests;

public interface IQuestProgressTracker
{
    /// <summary>
    /// Increments active quests for the metric. Auto-completes and awards reward badge.
    /// Does not call SaveChanges — caller owns the unit of work.
    /// </summary>
    Task ReportAsync(
        Guid userId,
        string metricCode,
        int delta = 1,
        CancellationToken cancellationToken = default);
}

internal sealed class QuestProgressTracker : IQuestProgressTracker
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IBadgeAwarder _badgeAwarder;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly TimeProvider _timeProvider;

    public QuestProgressTracker(
        IApplicationDbContext dbContext,
        IBadgeAwarder badgeAwarder,
        INotificationPublisher notificationPublisher,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _badgeAwarder = badgeAwarder;
        _notificationPublisher = notificationPublisher;
        _timeProvider = timeProvider;
    }

    public async Task ReportAsync(
        Guid userId,
        string metricCode,
        int delta = 1,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(metricCode) || delta <= 0)
        {
            return;
        }

        var normalizedMetric = metricCode.Trim().ToLowerInvariant();
        var utcNow = _timeProvider.GetUtcNow();

        var quests = await _dbContext.Quests
            .Where(quest => quest.IsActive && quest.MetricCode == normalizedMetric)
            .ToListAsync(cancellationToken);

        if (quests.Count == 0)
        {
            return;
        }

        var questIds = quests.Select(quest => quest.Id).ToList();
        var existing = await _dbContext.UserQuests
            .Where(userQuest =>
                userQuest.UserId == userId && questIds.Contains(userQuest.QuestId))
            .ToListAsync(cancellationToken);
        var byQuestId = existing.ToDictionary(userQuest => userQuest.QuestId);

        var rewardBadgeIds = quests.Select(quest => quest.RewardBadgeId).Distinct().ToList();
        var badges = await _dbContext.Badges.AsNoTracking()
            .Where(badge => rewardBadgeIds.Contains(badge.Id) && badge.IsActive)
            .ToDictionaryAsync(badge => badge.Id, cancellationToken);

        foreach (var quest in quests)
        {
            if (!byQuestId.TryGetValue(quest.Id, out var userQuest))
            {
                userQuest = UserQuest.Start(userId, quest.Id, utcNow);
                _dbContext.UserQuests.Add(userQuest);
                byQuestId[quest.Id] = userQuest;
            }

            var completedNow = userQuest.ReportProgress(delta, quest.TargetValue, utcNow);
            if (!completedNow)
            {
                continue;
            }

            if (!badges.TryGetValue(quest.RewardBadgeId, out var rewardBadge))
            {
                continue;
            }

            await _badgeAwarder.TryAwardAsync(userId, rewardBadge.Code, cancellationToken);

            await _notificationPublisher.PublishAsync(
                userId,
                NotificationType.QuestCompleted,
                "Görev tamamlandı",
                $"'{quest.Title}' görevini tamamladın.",
                NotificationEntityType.Quest,
                quest.Id,
                actorUserId: null,
                cancellationToken);
        }
    }
}
