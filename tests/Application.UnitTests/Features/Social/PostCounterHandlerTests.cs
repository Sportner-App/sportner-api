using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Sportner.Application.Abstractions.Gamification;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Storage;
using Sportner.Application.Features.Quests;
using Sportner.Application.Features.Social.Posts.CreatePost;
using Sportner.Application.Features.Social.Posts.DeletePost;
using Sportner.Application.Features.Social.Posts.LikePost;
using Sportner.Application.UnitTests.Infrastructure;

namespace Sportner.Application.UnitTests.Features.Social;

public sealed class PostCounterHandlerTests
{
    [Fact]
    public async Task CreateLikeDelete_UpdatesPostAndUserCounters()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));

        var author = TestUsers.CreateActive("+905551111111", time.GetUtcNow());
        var liker = TestUsers.CreateActive("+905552222222", time.GetUtcNow());
        db.Users.AddRange(author, liker);
        await db.SaveChangesAsync();

        var fileStorage = new Mock<IFileStorage>();
        var badgeAwarder = new Mock<IBadgeAwarder>();
        badgeAwarder
            .Setup(awarder => awarder.TryAwardAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var notifications = new Mock<INotificationPublisher>();
        notifications
            .Setup(publisher => publisher.PublishAsync(
                It.IsAny<Guid>(),
                It.IsAny<Domain.Common.Enums.NotificationType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Domain.Common.Enums.NotificationEntityType>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var createHandler = new CreatePostCommandHandler(
            db,
            new TestCurrentUser(author.Id),
            time,
            fileStorage.Object,
            badgeAwarder.Object,
            new Mock<IQuestProgressTracker>().Object);

        var created = await createHandler.Handle(
            new CreatePostCommand("Hello Sportner", Media: null),
            CancellationToken.None);

        created.IsSuccess.Should().BeTrue();
        author.Statistics!.PostsCount.Should().Be(1);

        var postId = created.Value!.Id;

        var likeHandler = new LikePostCommandHandler(
            db,
            new TestCurrentUser(liker.Id),
            time,
            notifications.Object);

        var liked = await likeHandler.Handle(new LikePostCommand(postId), CancellationToken.None);
        liked.IsSuccess.Should().BeTrue();

        var post = await db.Posts.FindAsync(postId);
        post!.LikeCount.Should().Be(1);

        var deleteHandler = new DeletePostCommandHandler(
            db,
            new TestCurrentUser(author.Id),
            time,
            fileStorage.Object);

        var deleted = await deleteHandler.Handle(new DeletePostCommand(postId), CancellationToken.None);
        deleted.IsSuccess.Should().BeTrue();
        author.Statistics.PostsCount.Should().Be(0);
        (await db.Posts.FindAsync(postId)).Should().BeNull();
    }
}
