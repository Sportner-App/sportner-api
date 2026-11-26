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
    public async Task ExplorePosts_OrdersNewestFirst_AndExcludesBlocked()
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
            db,
            new TestCurrentUser(viewer.Id),
            new Mock<IFileStorage>().Object);

        var result = await handler.Handle(new ExplorePostsQuery(20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Arkadaşlık artık sıralamayı etkilemiyor; yalnızca tarih belirliyor.
        result.Value!.Select(item => item.Id)
            .Should().ContainInOrder(strangerPost.Id, friendPost.Id);
        result.Value.Should().NotContain(item => item.Id == blockedPost.Id);
    }

    [Fact]
    public async Task ExplorePosts_FriendsOnly_ReturnsOnlyFriendPosts()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var viewer = CreateUserWithProfile(db, "viewer", "Viewer", now);
        var friend = CreateUserWithProfile(db, "friend", "Friend", now);
        var stranger = CreateUserWithProfile(db, "stranger", "Stranger", now);

        Accept(db, viewer.Id, friend.Id, now);

        var friendPost = Post.Create(friend.Id, "hi", now.AddHours(-2));
        var strangerPost = Post.Create(stranger.Id, "yo", now.AddMinutes(-10));
        db.Posts.AddRange(friendPost, strangerPost);
        await db.SaveChangesAsync();

        var handler = new ExplorePostsQueryHandler(
            db,
            new TestCurrentUser(viewer.Id),
            new Mock<IFileStorage>().Object);

        var result = await handler.Handle(
            new ExplorePostsQuery(20, FriendsOnly: true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().ContainSingle(item => item.Id == friendPost.Id);
        result.Value.Should().NotContain(item => item.Id == strangerPost.Id);
    }

    [Fact]
    public async Task ExplorePosts_Before_ReturnsNextPageWithoutOverlapOrGap()
    {
        await using var db = InMemoryDb.Create();
        var now = new FakeTimeProvider(new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero))
            .GetUtcNow();
        var viewer = CreateUserWithProfile(db, "viewer", "Viewer", now);
        var author = CreateUserWithProfile(db, "author", "Author", now);

        var posts = Enumerable.Range(0, 5)
            .Select(index => Post.Create(author.Id, $"p{index}", now.AddMinutes(-index)))
            .ToList();
        db.Posts.AddRange(posts);
        await db.SaveChangesAsync();

        var handler = CreatePostsHandler(db, viewer.Id);

        var first = await handler.Handle(new ExplorePostsQuery(2), CancellationToken.None);
        var second = await handler.Handle(
            new ExplorePostsQuery(2, Before: first.Value![^1].Id.ToString("D")),
            CancellationToken.None);
        var third = await handler.Handle(
            new ExplorePostsQuery(2, Before: second.Value![^1].Id.ToString("D")),
            CancellationToken.None);

        first.Value.Select(item => item.Id).Should().Equal(posts[0].Id, posts[1].Id);
        second.Value.Select(item => item.Id).Should().Equal(posts[2].Id, posts[3].Id);
        third.Value!.Select(item => item.Id).Should().Equal(posts[4].Id);
    }

    [Fact]
    public async Task ExplorePosts_Before_WithSameCreatedAt_DoesNotRepeatOrSkip()
    {
        await using var db = InMemoryDb.Create();
        var now = new FakeTimeProvider(new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero))
            .GetUtcNow();
        var viewer = CreateUserWithProfile(db, "viewer", "Viewer", now);
        var author = CreateUserWithProfile(db, "author", "Author", now);

        // InMemory Guid'leri Guid.CompareTo ile, imleç filtresi ise string olarak karşılaştırır;
        // iki sıra yalnızca ilk 4 bayt farklıysa örtüşür. Bu yüzden id'ler öyle üretilir
        // (PostgreSQL uuid sıralaması hex string sırasıyla aynıdır).
        var posts = Enumerable.Range(1, 5)
            .Select(index =>
            {
                var post = Post.Create(author.Id, $"p{index}", now);
                typeof(Post).GetProperty(nameof(Post.Id))!.SetValue(
                    post,
                    new Guid($"{index:x8}-0000-0000-0000-000000000000"));
                return post;
            })
            .ToList();
        db.Posts.AddRange(posts);
        await db.SaveChangesAsync();

        var handler = CreatePostsHandler(db, viewer.Id);
        var seen = new List<Guid>();
        string? before = null;
        for (var page = 0; page < 5; page++)
        {
            var result = await handler.Handle(
                new ExplorePostsQuery(2, Before: before),
                CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            if (result.Value!.Count == 0)
            {
                break;
            }

            seen.AddRange(result.Value.Select(item => item.Id));
            before = result.Value[^1].Id.ToString("D");
        }

        seen.Should().Equal(posts.OrderByDescending(post => post.Id).Select(post => post.Id));
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("11111111-1111-1111-1111-111111111111")]
    public async Task ExplorePosts_Before_InvalidOrUnknown_ReturnsInvalidCursor(string before)
    {
        await using var db = InMemoryDb.Create();
        var now = DateTimeOffset.UtcNow;
        var viewer = CreateUserWithProfile(db, "viewer", "Viewer", now);
        await db.SaveChangesAsync();

        var result = await CreatePostsHandler(db, viewer.Id).Handle(
            new ExplorePostsQuery(20, Before: before),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(error => error.Code == "Post.InvalidCursor");
    }

    [Fact]
    public async Task ExplorePosts_Before_WithFriendsOnly_PagesOnlyFriendPosts()
    {
        await using var db = InMemoryDb.Create();
        var now = new FakeTimeProvider(new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero))
            .GetUtcNow();
        var viewer = CreateUserWithProfile(db, "viewer", "Viewer", now);
        var friend = CreateUserWithProfile(db, "friend", "Friend", now);
        var stranger = CreateUserWithProfile(db, "stranger", "Stranger", now);
        Accept(db, viewer.Id, friend.Id, now);

        var newest = Post.Create(friend.Id, "a", now.AddMinutes(-1));
        var strangerPost = Post.Create(stranger.Id, "b", now.AddMinutes(-2));
        var older = Post.Create(friend.Id, "c", now.AddMinutes(-3));
        var oldest = Post.Create(friend.Id, "d", now.AddMinutes(-4));
        db.Posts.AddRange(newest, strangerPost, older, oldest);
        await db.SaveChangesAsync();

        var result = await CreatePostsHandler(db, viewer.Id).Handle(
            new ExplorePostsQuery(20, FriendsOnly: true, Before: newest.Id.ToString("D")),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Select(item => item.Id).Should().Equal(older.Id, oldest.Id);
    }

    private static ExplorePostsQueryHandler CreatePostsHandler(AppDbContext db, Guid viewerId) =>
        new(db, new TestCurrentUser(viewerId), new Mock<IFileStorage>().Object);

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
