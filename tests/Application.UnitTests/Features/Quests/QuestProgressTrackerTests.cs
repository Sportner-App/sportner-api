using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Sportner.Application.Abstractions.Gamification;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Features.Quests;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Badges;
using Sportner.Domain.Common.Constants;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Quests;
using Sportner.Domain.Users;
using Sportner.Infrastructure.Persistence;

namespace Sportner.Application.UnitTests.Features.Quests;

public sealed class QuestProgressTrackerTests
{
    [Fact]
    public async Task ReportAsync_IncrementsAndCompletesOnce_AwardsBadge()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var user = CreateUser(db, now);
        var badge = Badge.Create(
            BadgeCodes.FirstPost,
            "İlk Gönderi",
            "desc",
            "badges/first-post.png",
            BadgeCategory.Social,
            BadgeRarity.Common,
            25,
            1,
            now);
        db.Badges.Add(badge);

        var quest = Quest.Create(
            QuestCodes.Post5,
            "5 gönderi",
            "Beş gönderi oluştur.",
            QuestMetrics.PostsCreated,
            targetValue: 2,
            badge.Id,
            sortOrder: 1,
            now);
        db.Quests.Add(quest);
        await db.SaveChangesAsync();

        var badgeAwarder = new Mock<IBadgeAwarder>();
        badgeAwarder
            .Setup(x => x.TryAwardAsync(user.Id, BadgeCodes.FirstPost, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var notifications = new Mock<INotificationPublisher>();
        var tracker = new QuestProgressTracker(
            db,
            badgeAwarder.Object,
            notifications.Object,
            time);

        await tracker.ReportAsync(user.Id, QuestMetrics.PostsCreated, 1);
        await db.SaveChangesAsync();

        var progress = db.UserQuests.Single();
        progress.Status.Should().Be(QuestStatus.Active);
        progress.CurrentValue.Should().Be(1);
        badgeAwarder.Verify(
            x => x.TryAwardAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        await tracker.ReportAsync(user.Id, QuestMetrics.PostsCreated, 1);
        await db.SaveChangesAsync();

        progress = db.UserQuests.Single();
        progress.Status.Should().Be(QuestStatus.Completed);
        progress.CurrentValue.Should().Be(2);
        progress.CompletedAt.Should().NotBeNull();
        badgeAwarder.Verify(
            x => x.TryAwardAsync(user.Id, BadgeCodes.FirstPost, It.IsAny<CancellationToken>()),
            Times.Once);
        notifications.Verify(
            x => x.PublishAsync(
                user.Id,
                NotificationType.QuestCompleted,
                It.IsAny<string>(),
                It.IsAny<string>(),
                NotificationEntityType.Quest,
                quest.Id,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Further reports must not re-award.
        await tracker.ReportAsync(user.Id, QuestMetrics.PostsCreated, 1);
        await db.SaveChangesAsync();
        badgeAwarder.Verify(
            x => x.TryAwardAsync(user.Id, BadgeCodes.FirstPost, It.IsAny<CancellationToken>()),
            Times.Once);
        db.UserQuests.Should().HaveCount(1);
    }

    private static User CreateUser(AppDbContext db, DateTimeOffset utcNow)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = TestUsers.CreateActive($"+9055{suffix}", utcNow);
        db.Users.Add(user);
        return user;
    }
}
