using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Sportner.Application.Abstractions.Recommendations;
using Sportner.Application.Services.Recommendations;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Social;
using Sportner.Domain.Sports;
using Sportner.Domain.Users;
using Sportner.Infrastructure.Persistence;
using DomainEvent = Sportner.Domain.Events.Event;

namespace Sportner.Application.UnitTests.Services.Recommendations;

public sealed class RecommendationServiceTests
{
    [Fact]
    public async Task ScorePeople_RanksMutualFriendsAboveSameCity_WithDefaultWeights()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var viewer = CreateUserWithProfile(db, "viewer", "Viewer", now, city: "Ankara");
        var bridge = CreateUserWithProfile(db, "bridge", "Bridge", now, city: "Ankara");
        var fof = CreateUserWithProfile(db, "fof", "FoF", now, city: "Izmir");
        var cityOnly = CreateUserWithProfile(db, "cityonly", "CityOnly", now, city: "Ankara");

        Accept(db, viewer.Id, bridge.Id, now);
        Accept(db, bridge.Id, fof.Id, now);
        await db.SaveChangesAsync();

        var service = CreateService(db, time);
        var result = await service.ScorePeopleAsync(viewer.Id, 20);

        result.Should().HaveCountGreaterThanOrEqualTo(2);
        result[0].Item.UserId.Should().Be(fof.Id);
        result.Should().Contain(item => item.Item.UserId == cityOnly.Id);
        result.First(item => item.Item.UserId == fof.Id).Reasons
            .Should().Contain(reason => reason.StartsWith("mutualFriends:"));
    }

    [Fact]
    public async Task ScorePeople_RespectsConfiguredWeights()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var viewer = CreateUserWithProfile(db, "viewer", "Viewer", now, city: "Ankara");
        var bridge = CreateUserWithProfile(db, "bridge", "Bridge", now, city: "Ankara");
        var fof = CreateUserWithProfile(db, "fof", "FoF", now, city: "Izmir");
        var cityOnly = CreateUserWithProfile(db, "cityonly", "CityOnly", now, city: "Ankara");

        Accept(db, viewer.Id, bridge.Id, now);
        Accept(db, bridge.Id, fof.Id, now);
        await db.SaveChangesAsync();

        var options = new RecommendationOptions
        {
            People = new RecommendationOptions.PeopleWeights
            {
                MutualFriends = 0.1,
                SharedSports = 0,
                SameCity = 100,
                Reputation = 0,
                Activity = 0
            }
        };

        var service = CreateService(db, time, options);
        var result = await service.ScorePeopleAsync(viewer.Id, 20);

        result[0].Item.UserId.Should().Be(cityOnly.Id);
    }

    [Fact]
    public async Task ScorePeople_ExcludesBlockedAndBanned()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var viewer = CreateUserWithProfile(db, "viewer", "Viewer", now, city: "Ankara");
        var blocked = CreateUserWithProfile(db, "blocked", "Blocked", now, city: "Ankara");
        var banned = CreateUserWithProfile(db, "banned", "Banned", now, city: "Ankara");
        var ok = CreateUserWithProfile(db, "okuser", "Ok", now, city: "Ankara");

        db.UserBlocks.Add(UserBlock.Create(viewer.Id, blocked.Id, now));

        banned.Ban(now);
        await db.SaveChangesAsync();

        var result = await CreateService(db, time).ScorePeopleAsync(viewer.Id, 20);

        result.Select(item => item.Item.UserId).Should().Contain(ok.Id);
        result.Select(item => item.Item.UserId).Should().NotContain(blocked.Id);
        result.Select(item => item.Item.UserId).Should().NotContain(banned.Id);
    }

    [Fact]
    public async Task ScorePeople_ColdStart_ReturnsActivePublicProfiles()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var viewer = CreateUserWithProfile(db, "viewer", "Viewer", now);
        var stranger = CreateUserWithProfile(db, "stranger", "Stranger", now, city: "Bursa");
        await db.SaveChangesAsync();

        var result = await CreateService(db, time).ScorePeopleAsync(viewer.Id, 20);

        result.Should().ContainSingle(item => item.Item.UserId == stranger.Id);
        result[0].Reasons.Should().Contain("coldStart");
    }

    [Fact]
    public async Task ScoreEvents_PrefersSportMatchAndExcludesBlockedOrganizer()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var sportMatch = Sport.Create("Futbol", 1, now, "futbol");
        var otherSport = Sport.Create("Tenis", 2, now, "tenis");
        db.Sports.AddRange(sportMatch, otherSport);

        var viewer = CreateUserWithProfile(db, "viewer", "Viewer", now, city: "Istanbul");
        db.UserSports.Add(UserSport.Create(viewer.Id, sportMatch.Id, SkillLevel.Intermediate, now));

        var goodOrganizer = CreateUserWithProfile(db, "orggood", "OrgGood", now);
        var blockedOrganizer = CreateUserWithProfile(db, "orgbad", "OrgBad", now);

        db.UserBlocks.Add(UserBlock.Create(viewer.Id, blockedOrganizer.Id, now));

        var matched = CreatePublishedEvent(goodOrganizer.Id, sportMatch.Id, now.AddDays(2), 41.01m, 29.01m, now);
        var unmatched = CreatePublishedEvent(goodOrganizer.Id, otherSport.Id, now.AddDays(1), 41.02m, 29.02m, now);
        var blockedEvent = CreatePublishedEvent(blockedOrganizer.Id, sportMatch.Id, now.AddHours(12), 41.00m, 29.00m, now);

        db.Events.AddRange(matched, unmatched, blockedEvent);
        await db.SaveChangesAsync();

        var result = await CreateService(db, time).ScoreEventsAsync(
            viewer.Id,
            new EventRecommendationRequest(Limit: 20));

        result.Select(item => item.Item.EventId).Should().Contain(matched.Id);
        result.Select(item => item.Item.EventId).Should().Contain(unmatched.Id);
        result.Select(item => item.Item.EventId).Should().NotContain(blockedEvent.Id);
        result[0].Item.EventId.Should().Be(matched.Id);
        result.First(item => item.Item.EventId == matched.Id).Reasons.Should().Contain("sportMatch");
    }

    [Fact]
    public async Task ScorePosts_BoostsFriendAuthors_AndExcludesBlocked()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var viewer = CreateUserWithProfile(db, "viewer", "Viewer", now);
        var friend = CreateUserWithProfile(db, "friend", "Friend", now);
        var stranger = CreateUserWithProfile(db, "stranger", "Stranger", now);
        var blocked = CreateUserWithProfile(db, "blocked", "Blocked", now);

        Accept(db, viewer.Id, friend.Id, now);

        db.UserBlocks.Add(UserBlock.Create(viewer.Id, blocked.Id, now));

        var friendPost = Post.Create(friend.Id, "from friend", now.AddHours(-1));
        var strangerPost = Post.Create(stranger.Id, "from stranger", now.AddMinutes(-30));
        strangerPost.IncrementLikeCount(now, 20);
        var blockedPost = Post.Create(blocked.Id, "blocked", now);

        db.Posts.AddRange(friendPost, strangerPost, blockedPost);
        await db.SaveChangesAsync();

        var result = await CreateService(db, time).ScorePostsAsync(viewer.Id, 20);

        result.Select(item => item.Item.PostId).Should().NotContain(blockedPost.Id);
        result[0].Item.PostId.Should().Be(friendPost.Id);
        result[0].Reasons.Should().Contain("authorFriend");
    }

    private static RecommendationService CreateService(
        AppDbContext db,
        TimeProvider time,
        RecommendationOptions? options = null) =>
        new(
            db,
            Options.Create(options ?? new RecommendationOptions()),
            time,
            NullLogger<RecommendationService>.Instance);

    private static DomainEvent CreatePublishedEvent(
        Guid organizerId,
        Guid sportId,
        DateTimeOffset eventDate,
        decimal latitude,
        decimal longitude,
        DateTimeOffset utcNow)
    {
        var @event = DomainEvent.Create(
            organizerId,
            sportId,
            "Match",
            eventDate,
            durationMinutes: 90,
            latitude,
            longitude,
            address: "Istanbul",
            utcNow,
            maxParticipants: 10);
        @event.Publish(utcNow);
        return @event;
    }

    private static User CreateUserWithProfile(
        AppDbContext db,
        string username,
        string firstName,
        DateTimeOffset utcNow,
        string? city = null,
        bool isPublic = true)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = TestUsers.CreateActive($"+9055{suffix}", utcNow);
        var profile = UserProfile.Create(user.Id, username, firstName, utcNow, isProfilePublic: isPublic);
        if (city is not null)
        {
            profile.UpdateLocation(city, utcNow);
        }

        user.AttachUserProfile(profile);
        db.Users.Add(user);
        db.UserProfiles.Add(profile);
        return user;
    }

    private static void Accept(
        AppDbContext db,
        Guid requesterId,
        Guid addresseeId,
        DateTimeOffset utcNow)
    {
        var friendship = Friendship.CreateRequest(requesterId, addresseeId, utcNow);
        friendship.Accept(utcNow);
        db.Friendships.Add(friendship);
    }
}
