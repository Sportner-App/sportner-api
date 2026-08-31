using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Realtime;
using Sportner.Application.Features.Messaging.CreateDirectConversation;
using Sportner.Application.Features.Messaging.MarkConversationRead;
using Sportner.Application.Features.Messaging.MuteConversation;
using Sportner.Application.Features.Messaging.SearchMessages;
using Sportner.Application.Features.Messaging.SendTextMessage;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Messaging;
using Sportner.Domain.Social;
using Sportner.Domain.Users;
using Sportner.Infrastructure.Persistence;

namespace Sportner.Application.UnitTests.Features.Messaging;

public sealed class DmDepthHandlerTests
{
    [Fact]
    public async Task CreateDirect_AllowsStranger_ButBlocksBlockedUsers()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var me = CreateUser(db, "me", now);
        var stranger = CreateUser(db, "stranger", now);
        var blocked = CreateUser(db, "blocked", now);

        db.UserBlocks.Add(UserBlock.Create(me.Id, blocked.Id, now));
        await db.SaveChangesAsync();

        var handler = new CreateDirectConversationCommandHandler(
            db,
            new TestCurrentUser(me.Id),
            time);

        var ok = await handler.Handle(
            new CreateDirectConversationCommand(stranger.Id),
            CancellationToken.None);
        ok.IsSuccess.Should().BeTrue();

        var denied = await handler.Handle(
            new CreateDirectConversationCommand(blocked.Id),
            CancellationToken.None);
        denied.IsSuccess.Should().BeFalse();
        denied.Errors.Should().Contain(error => error.Code == "Messaging.Blocked");
    }

    [Fact]
    public async Task MarkRead_And_Mute_AffectMembership_And_SkipMutedNotifications()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var me = CreateUser(db, "me", now);
        var peer = CreateUser(db, "peer", now);
        var conversation = Conversation.CreateDirectConversation(me.Id, peer.Id, now);
        db.Conversations.Add(conversation);
        await db.SaveChangesAsync();

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

        var realtime = new Mock<IChatRealtimeNotifier>();
        realtime
            .Setup(notifier => notifier.NotifyMessageCreatedAsync(
                It.IsAny<Guid>(),
                It.IsAny<Application.Features.Messaging.MessageResponse>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Peer mutes conversation.
        var muteHandler = new MuteConversationCommandHandler(
            db,
            new TestCurrentUser(peer.Id),
            time);
        (await muteHandler.Handle(
            new MuteConversationCommand(conversation.Id),
            CancellationToken.None)).IsSuccess.Should().BeTrue();

        // Reload conversation with members for send (tracked).
        db.ChangeTracker.Clear();
        var sendHandler = new SendTextMessageCommandHandler(
            db,
            new TestCurrentUser(me.Id),
            time,
            notifications.Object,
            realtime.Object);

        var sent = await sendHandler.Handle(
            new SendTextMessageCommand(conversation.Id, "hello muted peer"),
            CancellationToken.None);
        sent.IsSuccess.Should().BeTrue();

        notifications.Verify(
            publisher => publisher.PublishAsync(
                peer.Id,
                NotificationType.NewMessage,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationEntityType>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        var markRead = new MarkConversationReadCommandHandler(
            db,
            new TestCurrentUser(peer.Id),
            time);

        (await markRead.Handle(
            new MarkConversationReadCommand(conversation.Id, sent.Value!.Id),
            CancellationToken.None)).IsSuccess.Should().BeTrue();

        var member = db.ConversationMembers.Single(candidate =>
            candidate.ConversationId == conversation.Id && candidate.UserId == peer.Id);
        member.LastReadMessageId.Should().Be(sent.Value.Id);
    }

    [Fact]
    public async Task SearchMessages_RequiresMembership()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var me = CreateUser(db, "me", now);
        var peer = CreateUser(db, "peer", now);
        var stranger = CreateUser(db, "stranger", now);
        var conversation = Conversation.CreateDirectConversation(me.Id, peer.Id, now);
        db.Conversations.Add(conversation);

        var message = Message.CreateText(conversation.Id, me.Id, "findme-secret", now);
        db.Messages.Add(message);
        await db.SaveChangesAsync();

        var allowed = new SearchMessagesQueryHandler(db, new TestCurrentUser(me.Id));
        var found = await allowed.Handle(
            new SearchMessagesQuery(conversation.Id, "secret"),
            CancellationToken.None);
        found.IsSuccess.Should().BeTrue();
        found.Value.Should().ContainSingle(item => item.Id == message.Id);

        var denied = new SearchMessagesQueryHandler(db, new TestCurrentUser(stranger.Id));
        var forbidden = await denied.Handle(
            new SearchMessagesQuery(conversation.Id, "secret"),
            CancellationToken.None);
        forbidden.IsSuccess.Should().BeFalse();
        forbidden.Errors.Should().Contain(error => error.Code == "Messaging.NotMember");
    }

    private static User CreateUser(AppDbContext db, string username, DateTimeOffset utcNow)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = TestUsers.CreateActive($"+9055{suffix}", utcNow);
        var profile = UserProfile.Create(user.Id, username, username, utcNow);
        user.AttachUserProfile(profile);
        db.Users.Add(user);
        db.UserProfiles.Add(profile);
        return user;
    }
}
