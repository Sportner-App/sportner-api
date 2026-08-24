using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Features.Events.ApproveParticipant;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Messaging;
using Sportner.Domain.Sports;
using DomainEvent = Sportner.Domain.Events.Event;

namespace Sportner.Application.UnitTests.Features.Events;

public sealed class ApproveParticipantCommandHandlerTests
{
    [Fact]
    public async Task Handle_ApprovesPendingParticipant_AndAddsConversationMember()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 24, 5, 0, 0, TimeSpan.Zero));

        var organizer = TestUsers.CreateActive("+905551111111", time.GetUtcNow());
        var applicant = TestUsers.CreateActive("+905552222222", time.GetUtcNow());
        var sport = Sport.Create("Futbol", 1, time.GetUtcNow(), "futbol");

        var @event = DomainEvent.Create(
            organizer.Id,
            sport.Id,
            "Halı saha",
            time.GetUtcNow().AddHours(4),
            durationMinutes: 90,
            latitude: 41m,
            longitude: 29m,
            address: "Istanbul",
            time.GetUtcNow(),
            maxParticipants: 10);

        @event.Publish(time.GetUtcNow());
        @event.Apply(applicant.Id, time.GetUtcNow());

        db.Users.AddRange(organizer, applicant);
        db.Sports.Add(sport);
        db.Events.Add(@event);
        db.Conversations.Add(
            Conversation.CreateEventConversation(
                @event.Id,
                organizer.Id,
                time.GetUtcNow(),
                @event.Title));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var publisher = new Mock<INotificationPublisher>();
        publisher
            .Setup(candidate => candidate.PublishAsync(
                It.IsAny<Guid>(),
                It.IsAny<NotificationType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationEntityType>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new ApproveParticipantCommandHandler(
            db,
            new TestCurrentUser(organizer.Id),
            time,
            publisher.Object);

        var result = await handler.Handle(
            new ApproveParticipantCommand(@event.Id, applicant.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(because: string.Join("; ", result.Errors.Select(e => e.Message)));

        var participant = await db.EventParticipants.AsNoTracking()
            .SingleAsync(row => row.EventId == @event.Id && row.UserId == applicant.Id);
        participant.Status.Should().Be(ParticipantStatus.Approved);

        var member = await db.ConversationMembers.AsNoTracking()
            .SingleAsync(row => row.UserId == applicant.Id);
        member.LeftAt.Should().BeNull();
    }
}
