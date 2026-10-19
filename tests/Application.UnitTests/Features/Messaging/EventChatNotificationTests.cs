using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Realtime;
using Sportner.Application.Features.Messaging.SendTextMessage;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Messaging;

namespace Sportner.Application.UnitTests.Features.Messaging;

public sealed class EventChatNotificationTests
{
    /// <summary>
    /// Bildirim yalnızca gönderenin adını taşıyordu. Birden fazla etkinliğin
    /// sohbetinde olan biri, mesajın hangi etkinlikten geldiğini bildirime
    /// bakarak anlayamıyordu.
    /// </summary>
    [Fact]
    public async Task SendTextMessage_PutsTheEventNameInTheTitle_ForEventChats()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var sender = TestUsers.CreateActive("+905551111111", now);
        var recipient = TestUsers.CreateActive("+905552222222", now);
        db.Users.AddRange(sender, recipient);

        var conversation = Conversation.CreateEventConversation(
            Guid.NewGuid(),
            sender.Id,
            now,
            "Sabah bisiklet antremanı");
        conversation.AddMember(recipient.Id, now);
        db.Conversations.Add(conversation);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        string? title = null;
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
            .Callback<Guid, NotificationType, string, string, NotificationEntityType, Guid?, Guid?, CancellationToken>(
                (_, _, publishedTitle, _, _, _, _, _) => title = publishedTitle)
            .Returns(Task.CompletedTask);

        var handler = new SendTextHandlerFactory(db, time, notifications).Create(sender.Id);

        var sent = await handler.Handle(
            new SendTextMessageCommand(conversation.Id, "yarın saat kaçta?"),
            CancellationToken.None);

        sent.IsSuccess.Should().BeTrue();
        title.Should().Contain("Sabah bisiklet antremanı");
    }

    private sealed class SendTextHandlerFactory
    {
        private readonly Sportner.Infrastructure.Persistence.AppDbContext _db;
        private readonly TimeProvider _time;
        private readonly Mock<INotificationPublisher> _notifications;

        internal SendTextHandlerFactory(
            Sportner.Infrastructure.Persistence.AppDbContext db,
            TimeProvider time,
            Mock<INotificationPublisher> notifications)
        {
            _db = db;
            _time = time;
            _notifications = notifications;
        }

        internal SendTextMessageCommandHandler Create(Guid senderId) =>
            new(
                _db,
                new TestCurrentUser(senderId),
                _time,
                _notifications.Object,
                Mock.Of<IChatRealtimeNotifier>());
    }
}
