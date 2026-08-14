using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Features.Gamification;
using Sportner.Application.Features.Gamification.GetMyBadgeProgress;
using Sportner.Application.Features.Gamification.ListBadges;
using Sportner.Application.Features.Gamification.SetShowcasedBadges;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Badges;
using Sportner.Domain.Common.Constants;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Users;
using Sportner.Infrastructure.Persistence;

namespace Sportner.Application.UnitTests.Features.Gamification;

public sealed class BadgeDepthHandlerTests
{
    [Fact]
    public async Task GetMyBadgeProgress_ReportsCurrentTowardEventMaster()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var user = CreateUser(db, now);
        SeedBadge(db, BadgeCodes.EventMaster, "Event Master", BadgeCategory.Events, now, displayOrder: 1);
        await db.SaveChangesAsync();

        // Progress uses attendance counts; without participants current stays 0.
        var handler = new GetMyBadgeProgressQueryHandler(db, new TestCurrentUser(user.Id));
        var result = await handler.Handle(new GetMyBadgeProgressQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var item = result.Value!.Single(candidate => candidate.Code == BadgeCodes.EventMaster);
        item.Current.Should().Be(0);
        item.Target.Should().Be(BadgeThresholds.EventMasterAttended);
        item.Percent.Should().Be(0);
        item.Earned.Should().BeFalse();
    }

    [Fact]
    public async Task SetShowcasedBadges_EnforcesMaxThreeAndOwnership()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var user = CreateUser(db, now);
        var other = CreateUser(db, now);

        var b1 = SeedBadge(db, "B1", "One", BadgeCategory.Social, now, 1);
        var b2 = SeedBadge(db, "B2", "Two", BadgeCategory.Social, now, 2);
        var b3 = SeedBadge(db, "B3", "Three", BadgeCategory.Social, now, 3);
        var b4 = SeedBadge(db, "B4", "Four", BadgeCategory.Social, now, 4);

        db.UserBadges.AddRange(
            UserBadge.Award(user.Id, b1.Id, now, now),
            UserBadge.Award(user.Id, b2.Id, now, now),
            UserBadge.Award(user.Id, b3.Id, now, now),
            UserBadge.Award(user.Id, b4.Id, now, now),
            UserBadge.Award(other.Id, b1.Id, now, now));
        await db.SaveChangesAsync();

        var handler = new SetShowcasedBadgesCommandHandler(db, new TestCurrentUser(user.Id), time);

        var tooMany = await handler.Handle(
            new SetShowcasedBadgesCommand([b1.Id, b2.Id, b3.Id, b4.Id]),
            CancellationToken.None);
        tooMany.IsSuccess.Should().BeFalse();
        tooMany.Errors.Should().Contain(error => error.Code == "Badge.ShowcaseTooMany");

        var notOwned = await handler.Handle(
            new SetShowcasedBadgesCommand([Guid.NewGuid()]),
            CancellationToken.None);
        notOwned.IsSuccess.Should().BeFalse();
        notOwned.Errors.Should().Contain(error => error.Code == "Badge.ShowcaseNotOwned");

        var ok = await handler.Handle(
            new SetShowcasedBadgesCommand([b3.Id, b1.Id]),
            CancellationToken.None);
        ok.IsSuccess.Should().BeTrue();
        ok.Value.Should().NotBeNull();
        ok.Value!.Where(item => item.IsShowcased).Should().HaveCount(2);
        ok.Value.First(item => item.BadgeId == b3.Id).ShowcaseOrder.Should().Be(1);
        ok.Value.First(item => item.BadgeId == b1.Id).ShowcaseOrder.Should().Be(2);
    }

    [Fact]
    public async Task ListBadges_FiltersEarnedAndCategory_WhenAuthenticated()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var user = CreateUser(db, now);
        var social = SeedBadge(db, "SOCIAL_X", "Social", BadgeCategory.Social, now, 1);
        SeedBadge(db, "EVENTS_X", "Events", BadgeCategory.Events, now, 2);
        db.UserBadges.Add(UserBadge.Award(user.Id, social.Id, now, now));
        await db.SaveChangesAsync();

        var handler = new ListBadgesQueryHandler(db, new TestCurrentUser(user.Id));

        var earnedOnly = await handler.Handle(
            new ListBadgesQuery(Earned: true),
            CancellationToken.None);
        earnedOnly.IsSuccess.Should().BeTrue();
        earnedOnly.Value.Should().ContainSingle(item => item.Code == "SOCIAL_X" && item.Earned == true);

        var socialCategory = await handler.Handle(
            new ListBadgesQuery(Category: (short)BadgeCategory.Social),
            CancellationToken.None);
        socialCategory.Value.Should().ContainSingle(item => item.Code == "SOCIAL_X");
    }

    [Fact]
    public async Task BadgeAwarder_AwardsSocialButterfly_AtThreshold()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var user = CreateUser(db, now);
        SeedBadge(db, BadgeCodes.SocialButterfly, "Butterfly", BadgeCategory.Social, now, 9);

        for (var i = 0; i < BadgeThresholds.SocialButterflyFriends; i++)
        {
            var friend = CreateUser(db, now);
            var friendship = Domain.Social.Friendship.CreateRequest(user.Id, friend.Id, now);
            friendship.Accept(now);
            db.Friendships.Add(friendship);
        }

        await db.SaveChangesAsync();

        var awarder = new BadgeAwarder(
            db,
            new Mock<INotificationPublisher>().Object,
            time,
            NullLogger<BadgeAwarder>.Instance);

        await awarder.EvaluateAfterFriendshipAcceptedAsync(user.Id);
        await db.SaveChangesAsync();

        db.UserBadges.Should().ContainSingle(item => item.UserId == user.Id);
    }

    private static User CreateUser(AppDbContext db, DateTimeOffset utcNow)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = TestUsers.CreateActive($"+9055{suffix}", utcNow);
        db.Users.Add(user);
        return user;
    }

    private static Badge SeedBadge(
        AppDbContext db,
        string code,
        string name,
        BadgeCategory category,
        DateTimeOffset utcNow,
        short displayOrder)
    {
        var badge = Badge.Create(
            code,
            name,
            $"{name} description",
            $"badges/{code.ToLowerInvariant()}.png",
            category,
            BadgeRarity.Common,
            50,
            displayOrder,
            utcNow);
        db.Badges.Add(badge);
        return badge;
    }
}
