using System.Globalization;
using FluentAssertions;
using Sportner.Application.Features.Gamification.ListBadges;
using Sportner.Application.Features.Gamification.ListMyBadges;
using Sportner.Application.Features.Quests.ListMyQuests;
using Sportner.Application.Features.Quests.ListQuests;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Badges;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Quests;
using Sportner.Domain.Users;

namespace Sportner.Application.UnitTests.Features.Gamification;

/// <summary>
/// Locks in that badge/quest catalog text follows the negotiated UI culture end to end,
/// mirroring the sport-catalog localization pattern (see CatalogLocalization).
/// </summary>
public sealed class BadgeAndQuestLocalizationTests
{
    [Fact]
    public async Task ListBadges_ReturnsEnglishText_WhenCultureIsEnglish()
    {
        await using var db = InMemoryDb.Create();
        var now = DateTimeOffset.UtcNow;

        db.Badges.Add(Badge.Create(
            "FIRST_EVENT", "İlk Etkinlik", "İlk etkinliğine katıldın.", "badges/first-event.png",
            BadgeCategory.Events, BadgeRarity.Common, 50, 1, now,
            nameEn: "First Event", descriptionEn: "You attended your first event."));
        await db.SaveChangesAsync();

        await WithCulture("en-US", async () =>
        {
            var handler = new ListBadgesQueryHandler(db, new TestCurrentUser(null));
            var result = await handler.Handle(new ListBadgesQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            var badge = result.Value!.Single();
            badge.Name.Should().Be("First Event");
            badge.Description.Should().Be("You attended your first event.");
        });
    }

    [Fact]
    public async Task ListBadges_FallsBackToTurkish_WhenEnglishTextMissing()
    {
        await using var db = InMemoryDb.Create();
        var now = DateTimeOffset.UtcNow;

        db.Badges.Add(Badge.Create(
            "FIRST_EVENT", "İlk Etkinlik", "İlk etkinliğine katıldın.", "badges/first-event.png",
            BadgeCategory.Events, BadgeRarity.Common, 50, 1, now));
        await db.SaveChangesAsync();

        await WithCulture("en-US", async () =>
        {
            var handler = new ListBadgesQueryHandler(db, new TestCurrentUser(null));
            var result = await handler.Handle(new ListBadgesQuery(), CancellationToken.None);

            result.Value!.Single().Name.Should().Be("İlk Etkinlik");
        });
    }

    [Fact]
    public async Task ListMyBadges_ReturnsEnglishText_WhenCultureIsEnglish()
    {
        await using var db = InMemoryDb.Create();
        var now = DateTimeOffset.UtcNow;

        var user = TestUsers.CreateActive("+905551112233", now);
        db.Users.Add(user);

        var badge = Badge.Create(
            "FIRST_EVENT", "İlk Etkinlik", "İlk etkinliğine katıldın.", "badges/first-event.png",
            BadgeCategory.Events, BadgeRarity.Common, 50, 1, now,
            nameEn: "First Event", descriptionEn: "You attended your first event.");
        db.Badges.Add(badge);
        db.UserBadges.Add(UserBadge.Award(user.Id, badge.Id, now, now));
        await db.SaveChangesAsync();

        await WithCulture("en-US", async () =>
        {
            var handler = new ListMyBadgesQueryHandler(db, new TestCurrentUser(user.Id));
            var result = await handler.Handle(new ListMyBadgesQuery(), CancellationToken.None);

            result.Value!.Single().Name.Should().Be("First Event");
        });
    }

    [Fact]
    public async Task ListQuests_ReturnsEnglishText_WhenCultureIsEnglish()
    {
        await using var db = InMemoryDb.Create();
        var now = DateTimeOffset.UtcNow;

        var badge = Badge.Create(
            "FIRST_EVENT", "İlk Etkinlik", "İlk etkinliğine katıldın.", "badges/first-event.png",
            BadgeCategory.Events, BadgeRarity.Common, 50, 1, now);
        db.Badges.Add(badge);
        db.Quests.Add(Quest.Create(
            "ATTEND_3", "3 etkinliğe katıl", "Üç etkinlikte katılımını onaylat.",
            "events_attended", 3, badge.Id, 1, now,
            titleEn: "Attend 3 events", descriptionEn: "Get your attendance confirmed at three events."));
        await db.SaveChangesAsync();

        await WithCulture("en-US", async () =>
        {
            var handler = new ListQuestsQueryHandler(db, new TestCurrentUser(null));
            var result = await handler.Handle(new ListQuestsQuery(), CancellationToken.None);

            var quest = result.Value!.Single();
            quest.Title.Should().Be("Attend 3 events");
            quest.Description.Should().Be("Get your attendance confirmed at three events.");
        });
    }

    [Fact]
    public async Task ListMyQuests_ReturnsEnglishText_WhenCultureIsEnglish()
    {
        await using var db = InMemoryDb.Create();
        var now = DateTimeOffset.UtcNow;

        var user = TestUsers.CreateActive("+905551112233", now);
        db.Users.Add(user);

        var badge = Badge.Create(
            "FIRST_EVENT", "İlk Etkinlik", "İlk etkinliğine katıldın.", "badges/first-event.png",
            BadgeCategory.Events, BadgeRarity.Common, 50, 1, now);
        db.Badges.Add(badge);

        var quest = Quest.Create(
            "ATTEND_3", "3 etkinliğe katıl", "Üç etkinlikte katılımını onaylat.",
            "events_attended", 3, badge.Id, 1, now,
            titleEn: "Attend 3 events", descriptionEn: "Get your attendance confirmed at three events.");
        db.Quests.Add(quest);
        db.UserQuests.Add(UserQuest.Start(user.Id, quest.Id, now));
        await db.SaveChangesAsync();

        await WithCulture("en-US", async () =>
        {
            var handler = new ListMyQuestsQueryHandler(db, new TestCurrentUser(user.Id));
            var result = await handler.Handle(new ListMyQuestsQuery(), CancellationToken.None);

            result.Value!.Single().Title.Should().Be("Attend 3 events");
        });
    }

    private static async Task WithCulture(string cultureName, Func<Task> assertion)
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
            await assertion();
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}
