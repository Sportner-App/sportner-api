using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Sportner.Application.Abstractions.Gamification;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Features.Social.Comments.CreateReply;
using Sportner.Application.Features.Social.Comments.ListReplies;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Social;
using Sportner.Domain.Users;
using Sportner.Infrastructure.Persistence;

namespace Sportner.Application.UnitTests.Features.Social;

public sealed class CommentReplyHandlerTests
{
    [Fact]
    public async Task CreateReply_ToRoot_SetsNoReplyToUser_AndIncrementsCounts()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var author = CreateUserWithProfile(db, "author", "Author", now);
        var commenter = CreateUserWithProfile(db, "commenter", "Commenter", now);
        var replier = CreateUserWithProfile(db, "replier", "Replier", now);
        var post = Post.Create(author.Id, "Foto", now);
        var root = PostComment.CreateRoot(post.Id, commenter.Id, "Guzel kare", now.AddMinutes(1));
        db.Posts.Add(post);
        db.PostComments.Add(root);
        await db.SaveChangesAsync();

        var notifications = CreateNotifications();
        var handler = new CreateReplyCommandHandler(
            db,
            new TestCurrentUser(replier.Id),
            time,
            notifications.Object,
            CreateBadgeAwarder().Object);

        var result = await handler.Handle(
            new CreateReplyCommand(post.Id, root.Id, "Katiliyorum"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ParentCommentId.Should().Be(root.Id);
        result.Value.ReplyToUserId.Should().BeNull();
        result.Value.ReplyToUsername.Should().BeNull();

        var persisted = db.PostComments.Single(comment => comment.Id == result.Value.Id);
        persisted.ParentCommentId.Should().Be(root.Id);
        persisted.ReplyToUserId.Should().BeNull();
        db.PostComments.Single(comment => comment.Id == root.Id).ReplyCount.Should().Be(1);
        db.Posts.Single(item => item.Id == post.Id).CommentCount.Should().Be(1);

        notifications.Verify(
            publisher => publisher.PublishAsync(
                commenter.Id,
                NotificationType.CommentReplied,
                "replier kullanıcısı yorumuna yanıt verdi",
                "Katiliyorum",
                NotificationEntityType.Comment,
                persisted.Id,
                replier.Id,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateReply_ToReply_FlattensUnderRoot_AndSetsReplyToUser()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var author = CreateUserWithProfile(db, "author", "Author", now);
        var commenter = CreateUserWithProfile(db, "commenter", "Commenter", now);
        var firstReplier = CreateUserWithProfile(db, "first", "First", now);
        var secondReplier = CreateUserWithProfile(db, "second", "Second", now);
        var post = Post.Create(author.Id, "Foto", now);
        var root = PostComment.CreateRoot(post.Id, commenter.Id, "Guzel kare", now.AddMinutes(1));
        var firstReply = PostComment.CreateReply(
            post.Id,
            firstReplier.Id,
            root.Id,
            "Evet",
            now.AddMinutes(2));
        root.IncrementReplyCount(now.AddMinutes(2));
        post.IncrementCommentCount(now.AddMinutes(2), amount: 2);
        db.Posts.Add(post);
        db.PostComments.AddRange(root, firstReply);
        await db.SaveChangesAsync();

        var notifications = CreateNotifications();
        var handler = new CreateReplyCommandHandler(
            db,
            new TestCurrentUser(secondReplier.Id),
            time,
            notifications.Object,
            CreateBadgeAwarder().Object);

        var result = await handler.Handle(
            new CreateReplyCommand(post.Id, firstReply.Id, "Sana da katiliyorum"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ParentCommentId.Should().Be(root.Id);
        result.Value.ReplyToUserId.Should().Be(firstReplier.Id);
        result.Value.ReplyToUsername.Should().Be("first");

        var persisted = db.PostComments.Single(comment => comment.Id == result.Value.Id);
        persisted.ParentCommentId.Should().Be(root.Id);
        persisted.ReplyToUserId.Should().Be(firstReplier.Id);
        persisted.IsReply().Should().BeTrue();
        db.PostComments.Single(comment => comment.Id == root.Id).ReplyCount.Should().Be(2);
        db.Posts.Single(item => item.Id == post.Id).CommentCount.Should().Be(3);

        notifications.Verify(
            publisher => publisher.PublishAsync(
                firstReplier.Id,
                NotificationType.CommentReplied,
                "second kullanıcısı yorumuna yanıt verdi",
                "Sana da katiliyorum",
                NotificationEntityType.Comment,
                persisted.Id,
                secondReplier.Id,
                It.IsAny<CancellationToken>()),
            Times.Once);
        notifications.Verify(
            publisher => publisher.PublishAsync(
                commenter.Id,
                It.IsAny<NotificationType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationEntityType>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ListReplies_ReturnsOnlyThatRoot_InCreatedAtAscendingOrder()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var author = CreateUserWithProfile(db, "author", "Author", now);
        var commenter = CreateUserWithProfile(db, "commenter", "Commenter", now);
        var other = CreateUserWithProfile(db, "other", "Other", now);
        var post = Post.Create(author.Id, "Foto", now);
        var root = PostComment.CreateRoot(post.Id, commenter.Id, "Ilk", now.AddMinutes(1));
        var otherRoot = PostComment.CreateRoot(post.Id, other.Id, "Baska", now.AddMinutes(2));
        var olderReply = PostComment.CreateReply(
            post.Id,
            author.Id,
            root.Id,
            "Eski yanit",
            now.AddMinutes(3));
        var newerReply = PostComment.CreateReply(
            post.Id,
            other.Id,
            root.Id,
            "Yeni yanit",
            now.AddMinutes(4),
            author.Id);
        var otherReply = PostComment.CreateReply(
            post.Id,
            commenter.Id,
            otherRoot.Id,
            "Baska thread",
            now.AddMinutes(5));
        db.Posts.Add(post);
        db.PostComments.AddRange(root, otherRoot, olderReply, newerReply, otherReply);
        await db.SaveChangesAsync();

        var handler = new ListRepliesQueryHandler(db, new TestCurrentUser(author.Id));
        var result = await handler.Handle(
            new ListRepliesQuery(post.Id, root.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.Items[0].Id.Should().Be(olderReply.Id);
        result.Value.Items[0].ReplyToUserId.Should().BeNull();
        result.Value.Items[1].Id.Should().Be(newerReply.Id);
        result.Value.Items[1].ReplyToUserId.Should().Be(author.Id);
        result.Value.Items[1].ReplyToUsername.Should().Be("author");
    }

    [Fact]
    public async Task ListReplies_WhenTargetIsReply_ReturnsCommentNotFound()
    {
        await using var db = InMemoryDb.Create();
        var now = new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero);
        var author = CreateUserWithProfile(db, "author", "Author", now);
        var commenter = CreateUserWithProfile(db, "commenter", "Commenter", now);
        var post = Post.Create(author.Id, "Foto", now);
        var root = PostComment.CreateRoot(post.Id, commenter.Id, "Ilk", now.AddMinutes(1));
        var reply = PostComment.CreateReply(post.Id, author.Id, root.Id, "Yanit", now.AddMinutes(2));
        db.Posts.Add(post);
        db.PostComments.AddRange(root, reply);
        await db.SaveChangesAsync();

        var handler = new ListRepliesQueryHandler(db, new TestCurrentUser(author.Id));
        var result = await handler.Handle(
            new ListRepliesQuery(post.Id, reply.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Code == "Post.CommentNotFound");
    }

    private static Mock<INotificationPublisher> CreateNotifications()
    {
        var notifications = new Mock<INotificationPublisher>();
        notifications
            .Setup(publisher => publisher.PublishAsync(
                It.IsAny<Guid>(),
                It.IsAny<NotificationType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationEntityType>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return notifications;
    }

    private static Mock<IBadgeAwarder> CreateBadgeAwarder()
    {
        var badgeAwarder = new Mock<IBadgeAwarder>();
        badgeAwarder
            .Setup(awarder => awarder.EvaluateAfterCommentCreatedAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return badgeAwarder;
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
