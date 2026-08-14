using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Sportner.Application.Features.Social.Friendships.GetFriendSuggestions;
using Sportner.Application.Features.Social.Friendships.GetMutualFriends;
using Sportner.Application.Features.Social.Friendships.SearchFriends;
using Sportner.Application.Services.Recommendations;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Social;
using Sportner.Domain.Sports;
using Sportner.Domain.Users;
using Sportner.Infrastructure.Persistence;

namespace Sportner.Application.UnitTests.Features.Social;

public sealed class FriendshipDepthHandlerTests
{
    [Fact]
    public async Task GetMutualFriends_ReturnsIntersection()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var viewer = CreateUserWithProfile(db, "viewer", "Viewer", now, city: "Istanbul");
        var target = CreateUserWithProfile(db, "target", "Target", now, city: "Istanbul");
        var mutual = CreateUserWithProfile(db, "mutual", "Mutual", now, city: "Istanbul");
        var onlyViewer = CreateUserWithProfile(db, "onlyviewer", "OnlyViewer", now, city: "Istanbul");
        var onlyTarget = CreateUserWithProfile(db, "onlytarget", "OnlyTarget", now, city: "Istanbul");

        Accept(db, viewer.Id, mutual.Id, now);
        Accept(db, target.Id, mutual.Id, now);
        Accept(db, viewer.Id, onlyViewer.Id, now);
        Accept(db, target.Id, onlyTarget.Id, now);
        await db.SaveChangesAsync();

        var handler = new GetMutualFriendsQueryHandler(db, new TestCurrentUser(viewer.Id));
        var result = await handler.Handle(new GetMutualFriendsQuery(target.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(1);
        result.Value.Items.Should().ContainSingle(item => item.UserId == mutual.Id);
    }

    [Fact]
    public async Task GetMutualFriends_WhenBlocked_Fails()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var viewer = CreateUserWithProfile(db, "viewer", "Viewer", now);
        var target = CreateUserWithProfile(db, "target", "Target", now);
        var blocked = Friendship.CreateRequest(viewer.Id, target.Id, now);
        blocked.Block(viewer.Id, now);
        db.Friendships.Add(blocked);
        await db.SaveChangesAsync();

        var handler = new GetMutualFriendsQueryHandler(db, new TestCurrentUser(viewer.Id));
        var result = await handler.Handle(new GetMutualFriendsQuery(target.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Code == "Friendship.Blocked");
    }

    [Fact]
    public async Task SearchFriends_OnlySearchesAcceptedFriends()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var viewer = CreateUserWithProfile(db, "viewer", "Viewer", now);
        var friend = CreateUserWithProfile(db, "ahmet", "Ahmet", now);
        var stranger = CreateUserWithProfile(db, "ahmetx", "AhmetX", now);
        Accept(db, viewer.Id, friend.Id, now);
        await db.SaveChangesAsync();

        var handler = new SearchFriendsQueryHandler(db, new TestCurrentUser(viewer.Id));
        var result = await handler.Handle(new SearchFriendsQuery("ahm"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(item => item.UserId == friend.Id);
        result.Value.Should().NotContain(item => item.UserId == stranger.Id);
    }

    [Fact]
    public async Task Suggestions_ExcludesFriendsPendingBlockedPrivateAndRecentRejects()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var sport = Sport.Create("Futbol", 1, now);
        db.Sports.Add(sport);

        var viewer = CreateUserWithProfile(db, "viewer", "Viewer", now, city: "Ankara");
        db.UserSports.Add(UserSport.Create(viewer.Id, sport.Id, SkillLevel.Intermediate, now));

        var bridge = CreateUserWithProfile(db, "bridge", "Bridge", now, city: "Ankara");
        var fof = CreateUserWithProfile(db, "fof", "FoF", now, city: "Izmir");
        var sharedSportUser = CreateUserWithProfile(db, "sporty", "Sporty", now, city: "Bursa");
        db.UserSports.Add(UserSport.Create(sharedSportUser.Id, sport.Id, SkillLevel.Beginner, now));

        var alreadyFriend = CreateUserWithProfile(db, "friend", "Friend", now, city: "Ankara");
        var pending = CreateUserWithProfile(db, "pending", "Pending", now, city: "Ankara");
        var privateUser = CreateUserWithProfile(db, "private", "Private", now, city: "Ankara", isPublic: false);
        var recentReject = CreateUserWithProfile(db, "rejected", "Rejected", now, city: "Ankara");

        Accept(db, viewer.Id, bridge.Id, now);
        Accept(db, bridge.Id, fof.Id, now);
        Accept(db, viewer.Id, alreadyFriend.Id, now);

        db.Friendships.Add(Friendship.CreateRequest(viewer.Id, pending.Id, now));

        var rejected = Friendship.CreateRequest(viewer.Id, recentReject.Id, now);
        rejected.Reject(now);
        db.Friendships.Add(rejected);

        // Same city would match, but private must be excluded.
        _ = privateUser;

        await db.SaveChangesAsync();

        var handler = new GetFriendSuggestionsQueryHandler(
            CreateRecommendationService(db, time),
            new TestCurrentUser(viewer.Id));

        var result = await handler.Handle(new GetFriendSuggestionsQuery(20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var ids = result.Value!.Select(item => item.UserId).ToList();

        ids.Should().Contain(fof.Id);
        ids.Should().Contain(sharedSportUser.Id);
        ids.Should().NotContain(alreadyFriend.Id);
        ids.Should().NotContain(pending.Id);
        ids.Should().NotContain(privateUser.Id);
        ids.Should().NotContain(recentReject.Id);
        ids.Should().NotContain(viewer.Id);
    }

    [Fact]
    public async Task Suggestions_IncludesRejectedAfterCooldown()
    {
        await using var db = InMemoryDb.Create();
        var rejectTime = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var now = new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);
        var time = new FakeTimeProvider(now);

        var viewer = CreateUserWithProfile(db, "viewer", "Viewer", now, city: "Ankara");
        var oldReject = CreateUserWithProfile(db, "oldreject", "OldReject", now, city: "Ankara");

        var rejected = Friendship.CreateRequest(viewer.Id, oldReject.Id, rejectTime);
        rejected.Reject(rejectTime);
        db.Friendships.Add(rejected);
        await db.SaveChangesAsync();

        var handler = new GetFriendSuggestionsQueryHandler(
            CreateRecommendationService(db, time),
            new TestCurrentUser(viewer.Id));

        var result = await handler.Handle(new GetFriendSuggestionsQuery(20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(item => item.UserId == oldReject.Id && item.SameCity);
    }

    private static RecommendationService CreateRecommendationService(
        AppDbContext db,
        TimeProvider time) =>
        new(
            db,
            Options.Create(new RecommendationOptions()),
            time,
            NullLogger<RecommendationService>.Instance);

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
