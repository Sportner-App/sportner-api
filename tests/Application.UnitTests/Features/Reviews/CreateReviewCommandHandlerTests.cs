using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Sportner.Application.Abstractions.Gamification;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Features.Quests;
using Sportner.Application.Features.Reviews.CreateReview;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Sports;
using DomainEvent = Sportner.Domain.Events.Event;

namespace Sportner.Application.UnitTests.Features.Reviews;

public sealed class CreateReviewCommandHandlerTests
{
    [Fact]
    public async Task Handle_NotifiesTheReviewedUser()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 24, 5, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var organizer = TestUsers.CreateActive("+905551110090", now);
        var attendee = TestUsers.CreateActive("+905551110091", now);
        var sport = Sport.Create("Futbol", 1, now, "futbol");

        var @event = DomainEvent.Create(
            organizer.Id, sport.Id, "Halı saha", now.AddHours(-2),
            durationMinutes: 90, latitude: 41m, longitude: 29m, address: "Istanbul",
            now.AddHours(-3), maxParticipants: 10);
        @event.Publish(now.AddHours(-3));
        @event.Apply(attendee.Id, now.AddHours(-2));
        @event.ApproveParticipant(attendee.Id, now.AddHours(-2));
        @event.Complete(now);
        @event.ConfirmAttendance(attendee.Id, now);

        db.Users.AddRange(organizer, attendee);
        db.Sports.Add(sport);
        db.Events.Add(@event);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        string? capturedTitle = null;
        string? capturedBody = null;
        var notifications = new Mock<INotificationPublisher>();
        notifications
            .Setup(p => p.PublishAsync(
                attendee.Id, NotificationType.ReviewReceived,
                It.IsAny<string>(), It.IsAny<string>(),
                NotificationEntityType.User, organizer.Id, organizer.Id,
                It.IsAny<CancellationToken>()))
            .Callback<Guid, NotificationType, string, string, NotificationEntityType, Guid?, Guid?, CancellationToken>(
                (_, _, title, body, _, _, _, _) =>
                {
                    capturedTitle = title;
                    capturedBody = body;
                })
            .Returns(Task.CompletedTask);

        var handler = new CreateReviewCommandHandler(
            db,
            new TestCurrentUser(organizer.Id),
            time,
            Mock.Of<IBadgeAwarder>(),
            Mock.Of<IQuestProgressTracker>(),
            notifications.Object);

        var result = await handler.Handle(
            new CreateReviewCommand(@event.Id, attendee.Id, 5, "Harikaydı"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(because: string.Join("; ", result.Errors.Select(e => e.Message)));
        notifications.VerifyAll();
        capturedTitle.Should().NotBeNullOrEmpty();
        capturedBody.Should().Be("\"Halı saha\" için yeni bir değerlendirme aldın.");
    }
}
