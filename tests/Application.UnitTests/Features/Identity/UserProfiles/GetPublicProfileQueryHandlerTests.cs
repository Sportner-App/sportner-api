using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Sportner.Application.Features.Identity.UserProfiles.GetPublicProfile;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Social;
using Sportner.Domain.Users;
using Sportner.Infrastructure.Persistence;

namespace Sportner.Application.UnitTests.Features.Identity.UserProfiles;

public sealed class GetPublicProfileQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenIncomingPending_ReturnsFriendshipForViewer()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 25, 12, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var requester = CreateUserWithProfile(db, "berkant", "Berkant", now);
        var addressee = CreateUserWithProfile(db, "viewer", "Viewer", now);
        var friendship = Friendship.CreateRequest(requester.Id, addressee.Id, now);
        db.Friendships.Add(friendship);
        await db.SaveChangesAsync();

        var handler = new GetPublicProfileQueryHandler(db, new TestCurrentUser(addressee.Id));
        var result = await handler.Handle(
            new GetPublicProfileQuery(requester.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Friendship.Should().NotBeNull();
        result.Value.Friendship!.FriendshipId.Should().Be(friendship.Id);
        result.Value.Friendship.Status.Should().Be((short)FriendshipStatus.Pending);
        result.Value.Friendship.RequesterUserId.Should().Be(requester.Id);
        result.Value.Friendship.AddresseeUserId.Should().Be(addressee.Id);
    }

    [Fact]
    public async Task Handle_WhenNoRelationship_ReturnsNullFriendship()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 25, 12, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var viewer = CreateUserWithProfile(db, "viewer", "Viewer", now);
        var other = CreateUserWithProfile(db, "other", "Other", now);
        await db.SaveChangesAsync();

        var handler = new GetPublicProfileQueryHandler(db, new TestCurrentUser(viewer.Id));
        var result = await handler.Handle(
            new GetPublicProfileQuery(other.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Friendship.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenViewingSelf_ReturnsNullFriendship()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 25, 12, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var viewer = CreateUserWithProfile(db, "viewer", "Viewer", now);
        await db.SaveChangesAsync();

        var handler = new GetPublicProfileQueryHandler(db, new TestCurrentUser(viewer.Id));
        var result = await handler.Handle(
            new GetPublicProfileQuery(viewer.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Friendship.Should().BeNull();
    }

    private static User CreateUserWithProfile(
        AppDbContext db,
        string username,
        string firstName,
        DateTimeOffset utcNow)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = TestUsers.CreateActive($"+9055{suffix}", utcNow);
        var profile = UserProfile.Create(user.Id, username, firstName, utcNow, isProfilePublic: true);
        user.AttachUserProfile(profile);
        db.Users.Add(user);
        db.UserProfiles.Add(profile);
        return user;
    }
}
