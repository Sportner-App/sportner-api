using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Sportner.Application.Abstractions.Storage;
using Sportner.Application.Features.Explore.ExploreEvents;
using Sportner.Application.Features.Explore.ExplorePeople;
using Sportner.Application.Features.Explore.ExplorePosts;
using Sportner.Application.Features.Identity.UserProfiles.DiscoverUsers;
using Sportner.Application.Services.Recommendations;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Social;
using Sportner.Domain.Sports;
using Sportner.Domain.Users;
using Sportner.Infrastructure.Persistence;
using DomainEvent = Sportner.Domain.Events.Event;

namespace Sportner.Application.UnitTests.Features.Explore;

public sealed class ExploreHandlerTests
{
    [Fact]
    public async Task DiscoverUsers_ReturnsPublicDirectory_AndExcludesViewerAndBlockedUsers()
    {
        await using var db = InMemoryDb.Create();
        var now = new DateTimeOffset(2026, 8, 27, 12, 0, 0, TimeSpan.Zero);

        var viewer = CreateUserWithProfile(db, "viewer", "Viewer", now);
        var friend = CreateUserWithProfile(db, "friend", "Friend", now, city: "İstanbul");
        friend.UserProfile!.UpdateAvatar("avatars/friend.jpg", now);
        var blocked = CreateUserWithProfile(db, "blocked", "Blocked", now);

        Accept(db, viewer.Id, friend.Id, now);
        db.UserBlocks.Add(UserBlock.Create(viewer.Id, blocked.Id, now));
        await db.SaveChangesAsync();

        var handler = new DiscoverUsersQueryHandler(db, new TestCurrentUser(viewer.Id));
        var result = await handler.Handle(
            new DiscoverUsersQuery(Page: 1, PageSize: 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var page = result.Value!;
        page.Items.Should().NotContain(item => item.UserId == viewer.Id);
        page.Items.Should().NotContain(item => item.UserId == blocked.Id);
        page.Items.Should().ContainSingle(item =>
            item.UserId == friend.Id && item.ProfileImageUrl == "avatars/friend.jpg");
        page.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task ExplorePeople_RequiresAuth_AndReturnsRanked()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var viewer = CreateUserWithProfile(db, "viewer", "Viewer", now, city: "Ankara");
        var peer = CreateUserWithProfile(db, "peer", "Peer", now, city: "Ankara");
        peer.UserProfile!.UpdateAvatar("avatars/peer.jpg", now);
        await db.SaveChangesAsync();

        var unauth = new ExplorePeopleQueryHandler(
            CreateRecommendationService(db, time),
            db,
            new TestCurrentUser(null));
        var unauthResult = await unauth.Handle(new ExplorePeopleQuery(), CancellationToken.None);
        unauthResult.IsSuccess.Should().BeFalse();
        unauthResult.Errors.Should().Contain(error => error.Code == "Explore.NotAuthenticated");

        var handler = new ExplorePeopleQueryHandler(
            CreateRecommendationService(db, time),
            db,
            new TestCurrentUser(viewer.Id));
        var result = await handler.Handle(new ExplorePeopleQuery(Limit: 10), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotContain(item => item.UserId == viewer.Id);
        result.Value.Should().Contain(item => item.UserId == peer.Id);
        result.Value.Single(item => item.UserId == peer.Id).ProfileImageUrl
            .Should().Be("avatars/peer.jpg");
    }

    [Fact]
    public async Task ExplorePeople_FiltersBySportId()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var futbol = Sport.Create("Futbol", 1, now, "futbol");
        var tenis = Sport.Create("Tenis", 2, now, "tenis");
        db.Sports.AddRange(futbol, tenis);

        var viewer = CreateUserWithProfile(db, "viewer", "Viewer", now, city: "Ankara");
        var footballer = CreateUserWithProfile(db, "foot", "Foot", now, city: "Ankara");
        var tennisPlayer = CreateUserWithProfile(db, "ten", "Ten", now, city: "Ankara");

        db.UserSports.Add(UserSport.Create(viewer.Id, futbol.Id, SkillLevel.Beginner, now));
        db.UserSports.Add(UserSport.Create(footballer.Id, futbol.Id, SkillLevel.Beginner, now));
        db.UserSports.Add(UserSport.Create(tennisPlayer.Id, tenis.Id, SkillLevel.Beginner, now));
        await db.SaveChangesAsync();

        var handler = new ExplorePeopleQueryHandler(
            CreateRecommendationService(db, time),
            db,
            new TestCurrentUser(viewer.Id));

        var result = await handler.Handle(
            new ExplorePeopleQuery(SportId: futbol.Id, Limit: 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(item => item.UserId == footballer.Id);
        result.Value.Should().NotContain(item => item.UserId == tennisPlayer.Id);
    }

    [Fact]
    public async Task ExploreEvents_ExcludesBlockedOrganizer_AndMarksSportMatch()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var sport = Sport.Create("Futbol", 1, now, "futbol");
        db.Sports.Add(sport);

        var viewer = CreateUserWithProfile(db, "viewer", "Viewer", now);
        db.UserSports.Add(UserSport.Create(viewer.Id, sport.Id, SkillLevel.Intermediate, now));

        var organizer = CreateUserWithProfile(db, "org", "Org", now);
        var blocked = CreateUserWithProfile(db, "bad", "Bad", now);

        db.UserBlocks.Add(UserBlock.Create(viewer.Id, blocked.Id, now));

        var good = CreatePublishedEvent(organizer.Id, sport.Id, now.AddDays(2), now);
        var bad = CreatePublishedEvent(blocked.Id, sport.Id, now.AddDays(1), now);
        db.Events.AddRange(good, bad);
        await db.SaveChangesAsync();

        var handler = new ExploreEventsQueryHandler(
            CreateRecommendationService(db, time),
            db,
            new TestCurrentUser(viewer.Id));

        var result = await handler.Handle(new ExploreEventsQuery(Limit: 20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(item => item.Id == good.Id && item.SportMatch);
        result.Value.Should().NotContain(item => item.Id == bad.Id);
    }

    [Fact]
    public async Task ExplorePosts_RanksFriendAboveStranger_AndExcludesBlocked()
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

        var friendPost = Post.Create(friend.Id, "hi", now.AddHours(-2));
        var strangerPost = Post.Create(stranger.Id, "yo", now.AddMinutes(-10));
        var blockedPost = Post.Create(blocked.Id, "nope", now);
        db.Posts.AddRange(friendPost, strangerPost, blockedPost);
        await db.SaveChangesAsync();

        var handler = new ExplorePostsQueryHandler(
            CreateRecommendationService(db, time),
            db,
            new TestCurrentUser(viewer.Id),
            new Mock<IFileStorage>().Object);

        var result = await handler.Handle(new ExplorePostsQuery(20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value![0].Id.Should().Be(friendPost.Id);
        result.Value.Should().NotContain(item => item.Id == blockedPost.Id);
    }

    private static RecommendationService CreateRecommendationService(
        AppDbContext db,
        TimeProvider time) =>
        new(
            db,
            Options.Create(new RecommendationOptions()),
            time,
            NullLogger<RecommendationService>.Instance);

    private static DomainEvent CreatePublishedEvent(
        Guid organizerId,
        Guid sportId,
        DateTimeOffset eventDate,
        DateTimeOffset utcNow)
    {
        var @event = DomainEvent.Create(
            organizerId,
            sportId,
            "Match",
            eventDate,
            durationMinutes: 90,
            latitude: 41m,
            longitude: 29m,
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
        string? city = null)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = TestUsers.CreateActive($"+9055{suffix}", utcNow);
        var profile = UserProfile.Create(user.Id, username, firstName, utcNow, isProfilePublic: true);
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
