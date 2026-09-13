using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Sportner.Application.Abstractions.Gamification;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Features.Events.CompleteEvent;
using Sportner.Application.Features.Events.ConfirmAttendance;
using Sportner.Application.Features.Quests;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Sports;
using DomainEvent = Sportner.Domain.Events.Event;

namespace Sportner.Application.UnitTests.Features.Events;

/// <summary>
/// The review-prompt notification is split across two moments: the organizer is notified when
/// the event completes (they're immediately review-eligible), and each participant is notified
/// separately when their OWN attendance is confirmed — domain rules require that to happen
/// after completion, so at completion time no participant is review-eligible yet.
/// </summary>
public sealed class CompleteEventCommandHandlerTests
{
    [Fact]
    public async Task Handle_NotifiesOrganizer_WhenEventCompletes()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 24, 5, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var organizer = TestUsers.CreateActive("+905551110040", now);
        var sport = Sport.Create("Futbol", 1, now, "futbol");

        var @event = DomainEvent.Create(
            organizer.Id, sport.Id, "Halı saha", now.AddHours(-2),
            durationMinutes: 90, latitude: 41m, longitude: 29m, address: "Istanbul",
            now.AddHours(-3), maxParticipants: 10);
        @event.Publish(now.AddHours(-3));

        db.Users.Add(organizer);
        db.Sports.Add(sport);
        db.Events.Add(@event);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        string? capturedTitle = null;
        var notifications = new Mock<INotificationPublisher>();
        notifications
            .Setup(p => p.PublishAsync(
                organizer.Id, NotificationType.EventReviewPrompt,
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<NotificationEntityType>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, NotificationType, string, string, NotificationEntityType, Guid?, Guid?, CancellationToken>(
                (_, _, title, _, _, _, _, _) => capturedTitle = title)
            .Returns(Task.CompletedTask);

        var handler = new CompleteEventCommandHandler(
            db,
            new TestCurrentUser(organizer.Id),
            time,
            Mock.Of<IBadgeAwarder>(),
            Mock.Of<IQuestProgressTracker>(),
            notifications.Object);

        var result = await handler.Handle(new CompleteEventCommand(@event.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue(because: string.Join("; ", result.Errors.Select(e => e.Message)));
        capturedTitle.Should().Be("Katılımcıları değerlendir");
    }

    [Fact]
    public async Task ConfirmAttendance_NotifiesThatParticipant_InTheirOwnLanguage()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 24, 5, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var organizer = TestUsers.CreateActive("+905551110050", now);
        var englishAttendee = TestUsers.CreateActive("+905551110051", now);
        var neverConfirmed = TestUsers.CreateActive("+905551110052", now);
        englishAttendee.SetPreferredLanguage(Language.English, now);
        var sport = Sport.Create("Futbol", 1, now, "futbol");

        var @event = DomainEvent.Create(
            organizer.Id, sport.Id, "Halı saha", now.AddHours(-2),
            durationMinutes: 90, latitude: 41m, longitude: 29m, address: "Istanbul",
            now.AddHours(-3), maxParticipants: 10);
        @event.Publish(now.AddHours(-3));
        @event.Apply(englishAttendee.Id, now.AddHours(-2));
        @event.ApproveParticipant(englishAttendee.Id, now.AddHours(-2));
        @event.Apply(neverConfirmed.Id, now.AddHours(-2));
        @event.ApproveParticipant(neverConfirmed.Id, now.AddHours(-2));
        @event.Complete(now);

        db.Users.AddRange(organizer, englishAttendee, neverConfirmed);
        db.Sports.Add(sport);
        db.Events.Add(@event);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var capturedByRecipient = new Dictionary<Guid, (string Title, string Body)>();
        var notifications = new Mock<INotificationPublisher>();
        notifications
            .Setup(p => p.PublishAsync(
                It.IsAny<Guid>(), NotificationType.EventReviewPrompt,
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<NotificationEntityType>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, NotificationType, string, string, NotificationEntityType, Guid?, Guid?, CancellationToken>(
                (recipientId, _, title, body, _, _, _, _) => capturedByRecipient[recipientId] = (title, body))
            .Returns(Task.CompletedTask);

        var handler = new ConfirmAttendanceCommandHandler(
            db,
            new TestCurrentUser(organizer.Id),
            time,
            Mock.Of<IBadgeAwarder>(),
            Mock.Of<IQuestProgressTracker>(),
            notifications.Object);

        var result = await handler.Handle(
            new ConfirmAttendanceCommand(@event.Id, englishAttendee.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(because: string.Join("; ", result.Errors.Select(e => e.Message)));

        capturedByRecipient[englishAttendee.Id].Title.Should().Be("Rate your teammates");
        capturedByRecipient[englishAttendee.Id].Body.Should().Be(
            "\"Halı saha\" is over — share your feedback.");
        capturedByRecipient.Should().NotContainKey(neverConfirmed.Id);
    }
}
