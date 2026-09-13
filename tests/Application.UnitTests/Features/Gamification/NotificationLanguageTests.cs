using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Sportner.Application.Abstractions.Gamification;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Features.Gamification;
using Sportner.Application.Features.Quests;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Badges;
using Sportner.Domain.Common.Constants;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Quests;
using Sportner.Infrastructure.Persistence;

namespace Sportner.Application.UnitTests.Features.Gamification;

/// <summary>
/// Locks in that badge/quest notifications are generated in the RECIPIENT's stored
/// PreferredLanguage — never the ambient request culture, since these can fire from a
/// background job with no HTTP request at all.
/// </summary>
public sealed class NotificationLanguageTests
{
    [Fact]
    public async Task BadgeAwarder_UsesEnglishNotificationText_WhenUserPrefersEnglish()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var user = TestUsers.CreateActive("+905551110001", now);
        user.SetPreferredLanguage(Language.English, now);
        db.Users.Add(user);

        db.Badges.Add(Badge.Create(
            BadgeCodes.FirstPost, "İlk Gönderi", "İlk gönderini paylaştın.", "badges/first-post.png",
            BadgeCategory.Social, BadgeRarity.Common, 25, 1, now,
            nameEn: "First Post", descriptionEn: "You shared your first post."));
        await db.SaveChangesAsync();

        string? capturedTitle = null;
        string? capturedBody = null;
        var notifications = new Mock<INotificationPublisher>();
        notifications
            .Setup(x => x.PublishAsync(
                user.Id,
                NotificationType.BadgeEarned,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationEntityType>(),
                It.IsAny<Guid?>(),
                null,
                It.IsAny<CancellationToken>()))
            .Callback<Guid, NotificationType, string, string, NotificationEntityType, Guid?, Guid?, CancellationToken>(
                (_, _, title, body, _, _, _, _) =>
                {
                    capturedTitle = title;
                    capturedBody = body;
                })
            .Returns(Task.CompletedTask);

        var awarder = new BadgeAwarder(db, notifications.Object, time, NullLogger<BadgeAwarder>.Instance);

        await awarder.TryAwardAsync(user.Id, BadgeCodes.FirstPost);

        capturedTitle.Should().Be("You earned a badge");
        capturedBody.Should().Be("You earned the 'First Post' badge.");
    }

    [Fact]
    public async Task BadgeAwarder_UsesTurkishNotificationText_ByDefault()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var user = TestUsers.CreateActive("+905551110002", now);
        db.Users.Add(user);

        db.Badges.Add(Badge.Create(
            BadgeCodes.FirstPost, "İlk Gönderi", "İlk gönderini paylaştın.", "badges/first-post.png",
            BadgeCategory.Social, BadgeRarity.Common, 25, 1, now,
            nameEn: "First Post", descriptionEn: "You shared your first post."));
        await db.SaveChangesAsync();

        string? capturedTitle = null;
        string? capturedBody = null;
        var notifications = new Mock<INotificationPublisher>();
        notifications
            .Setup(x => x.PublishAsync(
                user.Id,
                NotificationType.BadgeEarned,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationEntityType>(),
                It.IsAny<Guid?>(),
                null,
                It.IsAny<CancellationToken>()))
            .Callback<Guid, NotificationType, string, string, NotificationEntityType, Guid?, Guid?, CancellationToken>(
                (_, _, title, body, _, _, _, _) =>
                {
                    capturedTitle = title;
                    capturedBody = body;
                })
            .Returns(Task.CompletedTask);

        var awarder = new BadgeAwarder(db, notifications.Object, time, NullLogger<BadgeAwarder>.Instance);

        await awarder.TryAwardAsync(user.Id, BadgeCodes.FirstPost);

        capturedTitle.Should().Be("Rozet kazandın");
        capturedBody.Should().Be("'İlk Gönderi' rozetini kazandın.");
    }

    [Fact]
    public async Task QuestProgressTracker_UsesEnglishNotificationText_WhenUserPrefersEnglish()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var user = TestUsers.CreateActive("+905551110003", now);
        user.SetPreferredLanguage(Language.English, now);
        db.Users.Add(user);

        var badge = Badge.Create(
            BadgeCodes.FirstPost, "İlk Gönderi", "desc", "badges/first-post.png",
            BadgeCategory.Social, BadgeRarity.Common, 25, 1, now);
        db.Badges.Add(badge);

        var quest = Quest.Create(
            QuestCodes.Post5, "5 gönderi paylaş", "Beş gönderi oluştur.",
            QuestMetrics.PostsCreated, targetValue: 1, badge.Id, sortOrder: 1, now,
            titleEn: "Share 5 posts", descriptionEn: "Create five posts.");
        db.Quests.Add(quest);
        await db.SaveChangesAsync();

        string? capturedTitle = null;
        string? capturedBody = null;
        var notifications = new Mock<INotificationPublisher>();
        notifications
            .Setup(x => x.PublishAsync(
                user.Id,
                NotificationType.QuestCompleted,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationEntityType>(),
                It.IsAny<Guid?>(),
                null,
                It.IsAny<CancellationToken>()))
            .Callback<Guid, NotificationType, string, string, NotificationEntityType, Guid?, Guid?, CancellationToken>(
                (_, _, title, body, _, _, _, _) =>
                {
                    capturedTitle = title;
                    capturedBody = body;
                })
            .Returns(Task.CompletedTask);

        var badgeAwarder = new Mock<IBadgeAwarder>();
        badgeAwarder
            .Setup(x => x.TryAwardAsync(user.Id, badge.Code, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var tracker = new QuestProgressTracker(db, badgeAwarder.Object, notifications.Object, time);

        await tracker.ReportAsync(user.Id, QuestMetrics.PostsCreated, 1);

        capturedTitle.Should().Be("Quest completed");
        capturedBody.Should().Be("You completed the 'Share 5 posts' quest.");
    }
}
