using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Sportner.Application.Features.Identity.UserProfiles.GetPublicProfile;
using Sportner.Application.Features.Social.Blocks.BlockUser;
using Sportner.Application.Features.Social.Blocks.ListBlockedUsers;
using Sportner.Application.Features.Social.Blocks.UnblockUser;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Social;
using Sportner.Domain.Users;
using Sportner.Infrastructure.Persistence;

namespace Sportner.Application.UnitTests.Features.Social;

public sealed class BlockHandlerTests
{
    [Fact]
    public async Task Block_Stranger_CreatesRow_AndIsIdempotent()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var blocker = CreateUserWithProfile(db, "blocker", "Blocker", now);
        var target = CreateUserWithProfile(db, "target", "Target", now);
        await db.SaveChangesAsync();

        var handler = new BlockUserCommandHandler(db, new TestCurrentUser(blocker.Id), time);
        var first = await handler.Handle(new BlockUserCommand(target.Id), CancellationToken.None);
        var second = await handler.Handle(new BlockUserCommand(target.Id), CancellationToken.None);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        db.UserBlocks.Should().ContainSingle(block =>
            block.BlockerUserId == blocker.Id && block.BlockedUserId == target.Id);
        db.Friendships.Should().BeEmpty();
    }

    [Fact]
    public async Task Block_AcceptedFriend_RemovesFriendship_AndDecreasesCounts()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var blocker = CreateUserWithProfile(db, "blocker", "Blocker", now);
        var friend = CreateUserWithProfile(db, "friend", "Friend", now);
        var friendship = Friendship.CreateRequest(blocker.Id, friend.Id, now);
        friendship.Accept(now);
        db.Friendships.Add(friendship);
        blocker.Statistics!.IncreaseFriendsCount(now);
        friend.Statistics!.IncreaseFriendsCount(now);
        await db.SaveChangesAsync();

        var handler = new BlockUserCommandHandler(db, new TestCurrentUser(blocker.Id), time);
        var result = await handler.Handle(new BlockUserCommand(friend.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        db.Friendships.Should().BeEmpty();
        blocker.Statistics.FriendsCount.Should().Be(0);
        friend.Statistics.FriendsCount.Should().Be(0);
    }

    [Fact]
    public async Task Block_AllowsMutualIndependentRows()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var a = CreateUserWithProfile(db, "alice", "Alice", now);
        var b = CreateUserWithProfile(db, "bob", "Bob", now);
        await db.SaveChangesAsync();

        var fromA = new BlockUserCommandHandler(db, new TestCurrentUser(a.Id), time);
        var fromB = new BlockUserCommandHandler(db, new TestCurrentUser(b.Id), time);

        (await fromA.Handle(new BlockUserCommand(b.Id), CancellationToken.None)).IsSuccess.Should().BeTrue();
        (await fromB.Handle(new BlockUserCommand(a.Id), CancellationToken.None)).IsSuccess.Should().BeTrue();

        db.UserBlocks.Should().HaveCount(2);
    }

    [Fact]
    public async Task Unblock_DeletesOnlyOwnRow()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var a = CreateUserWithProfile(db, "alice", "Alice", now);
        var b = CreateUserWithProfile(db, "bob", "Bob", now);
        db.UserBlocks.Add(UserBlock.Create(a.Id, b.Id, now));
        db.UserBlocks.Add(UserBlock.Create(b.Id, a.Id, now));
        await db.SaveChangesAsync();

        var handler = new UnblockUserCommandHandler(db, new TestCurrentUser(a.Id));
        var result = await handler.Handle(new UnblockUserCommand(b.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        db.UserBlocks.Should().ContainSingle(block =>
            block.BlockerUserId == b.Id && block.BlockedUserId == a.Id);
    }

    [Fact]
    public async Task Unblock_WhenMissing_IsIdempotent()
    {
        await using var db = InMemoryDb.Create();
        var handler = new UnblockUserCommandHandler(db, new TestCurrentUser(Guid.NewGuid()));
        var result = await handler.Handle(new UnblockUserCommand(Guid.NewGuid()), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task List_ReturnsOnlyUsersIBlocked()
    {
        await using var db = InMemoryDb.Create();
        var now = new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero);

        var me = CreateUserWithProfile(db, "me", "Me", now);
        var listed = CreateUserWithProfile(db, "listed", "Listed", now);
        var other = CreateUserWithProfile(db, "other", "Other", now);
        db.UserBlocks.Add(UserBlock.Create(me.Id, listed.Id, now));
        db.UserBlocks.Add(UserBlock.Create(other.Id, me.Id, now.AddMinutes(1)));
        await db.SaveChangesAsync();

        var handler = new ListBlockedUsersQueryHandler(db, new TestCurrentUser(me.Id));
        var result = await handler.Handle(new ListBlockedUsersQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle(item => item.UserId == listed.Id);
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Block_Self_Fails()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero));
        var userId = Guid.NewGuid();

        var handler = new BlockUserCommandHandler(db, new TestCurrentUser(userId), time);
        var result = await handler.Handle(new BlockUserCommand(userId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Code == "Block.SelfBlock");
    }

    [Fact]
    public async Task GetPublicProfile_WhenEitherWayBlocked_ReturnsNotFound()
    {
        await using var db = InMemoryDb.Create();
        var now = new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero);

        var viewer = CreateUserWithProfile(db, "viewer", "Viewer", now);
        var target = CreateUserWithProfile(db, "target", "Target", now);
        db.UserBlocks.Add(UserBlock.Create(target.Id, viewer.Id, now));
        await db.SaveChangesAsync();

        var handler = new GetPublicProfileQueryHandler(db, new TestCurrentUser(viewer.Id));
        var result = await handler.Handle(new GetPublicProfileQuery(target.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Code == "Profile.NotFound");
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
