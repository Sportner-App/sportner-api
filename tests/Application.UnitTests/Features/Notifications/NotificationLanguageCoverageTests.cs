using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Features.Events.ApproveParticipant;
using Sportner.Application.Features.Events.CancelEvent;
using Sportner.Application.Features.Social.Friendships.SendFriendRequest;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Messaging;
using Sportner.Domain.Sports;
using Sportner.Domain.Users;
using DomainEvent = Sportner.Domain.Events.Event;

namespace Sportner.Application.UnitTests.Features.Notifications;

/// <summary>
/// Spot-checks the notification-localization pattern rolled out across ~20 producers
/// (see NotificationActor + NotificationsResource): actor-prefix fallback in both
/// languages, event-title body substitution, and — the highest-risk behavior — that a
/// single fan-out call renders DIFFERENT text per recipient based on each one's own
/// PreferredLanguage, not a single ambient culture.
/// </summary>
public sealed class NotificationLanguageCoverageTests
{
    [Fact]
    public async Task SendFriendRequest_FallsBackToLanguageAppropriateActorName_WhenActorHasNoProfile()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var now = time.GetUtcNow();

        var requester = TestUsers.CreateActive("+905551110010", now);
        var addressee = TestUsers.CreateActive("+905551110011", now);
        addressee.SetPreferredLanguage(Language.English, now);
        db.Users.AddRange(requester, addressee);
        await db.SaveChangesAsync();

        string? capturedText = null;
        var publisher = new Mock<INotificationPublisher>();
        publisher
            .Setup(p => p.PublishAsync(
                addressee.Id, NotificationType.FriendRequest,
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<NotificationEntityType>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, NotificationType, string, string, NotificationEntityType, Guid?, Guid?, CancellationToken>(
                (_, _, title, _, _, _, _, _) => capturedText = title)
            .Returns(Task.CompletedTask);

        var handler = new SendFriendRequestCommandHandler(
            db, new TestCurrentUser(requester.Id), time, publisher.Object);

        var result = await handler.Handle(
            new SendFriendRequestCommand(addressee.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // requester has no UserProfile row → falls back to the language-appropriate "no name" phrase.
        capturedText.Should().Be("A user sent you a friend request");
    }

    [Fact]
    public async Task ApproveParticipant_SubstitutesEventTitle_InEnglishBody()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 24, 5, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var organizer = TestUsers.CreateActive("+905551110020", now);
        var applicant = TestUsers.CreateActive("+905551110021", now);
        applicant.SetPreferredLanguage(Language.English, now);
        var sport = Sport.Create("Futbol", 1, now, "futbol");

        var @event = DomainEvent.Create(
            organizer.Id, sport.Id, "Halı saha", now.AddHours(4),
            durationMinutes: 90, latitude: 41m, longitude: 29m, address: "Istanbul",
            now, maxParticipants: 10);
        @event.Publish(now);
        @event.Apply(applicant.Id, now);

        var applicantProfile = UserProfile.Create(applicant.Id, "applicant-user", "Applicant", now);
        applicantProfile.UpdatePersonalDetails(1, new DateOnly(1995, 1, 1), now);

        db.Users.AddRange(organizer, applicant);
        db.UserProfiles.Add(applicantProfile);
        db.Sports.Add(sport);
        db.Events.Add(@event);
        db.Conversations.Add(
            Conversation.CreateEventConversation(@event.Id, organizer.Id, now, @event.Title));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        string? capturedBody = null;
        var publisher = new Mock<INotificationPublisher>();
        publisher
            .Setup(p => p.PublishAsync(
                It.IsAny<Guid>(), It.IsAny<NotificationType>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<NotificationEntityType>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, NotificationType, string, string, NotificationEntityType, Guid?, Guid?, CancellationToken>(
                (_, _, _, body, _, _, _, _) => capturedBody = body)
            .Returns(Task.CompletedTask);

        var handler = new ApproveParticipantCommandHandler(
            db, new TestCurrentUser(organizer.Id), time, publisher.Object);

        var result = await handler.Handle(
            new ApproveParticipantCommand(@event.Id, applicant.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue(because: string.Join("; ", result.Errors.Select(e => e.Message)));
        capturedBody.Should().Be("Your participation in \"Halı saha\" was approved.");
    }

    [Fact]
    public async Task CancelEvent_RendersEachRecipientsNotification_InTheirOwnLanguage()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 24, 5, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var organizer = TestUsers.CreateActive("+905551110030", now);
        var turkishAttendee = TestUsers.CreateActive("+905551110031", now);
        var englishAttendee = TestUsers.CreateActive("+905551110032", now);
        englishAttendee.SetPreferredLanguage(Language.English, now);
        var sport = Sport.Create("Futbol", 1, now, "futbol");

        var @event = DomainEvent.Create(
            organizer.Id, sport.Id, "Halı saha", now.AddHours(4),
            durationMinutes: 90, latitude: 41m, longitude: 29m, address: "Istanbul",
            now, maxParticipants: 10);
        @event.Publish(now);
        @event.Apply(turkishAttendee.Id, now);
        @event.ApproveParticipant(turkishAttendee.Id, now);
        @event.Apply(englishAttendee.Id, now);
        @event.ApproveParticipant(englishAttendee.Id, now);

        db.Users.AddRange(organizer, turkishAttendee, englishAttendee);
        db.Sports.Add(sport);
        db.Events.Add(@event);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var capturedByRecipient = new Dictionary<Guid, (string Title, string Body)>();
        var publisher = new Mock<INotificationPublisher>();
        publisher
            .Setup(p => p.PublishAsync(
                It.IsAny<Guid>(), NotificationType.EventCancelled,
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<NotificationEntityType>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, NotificationType, string, string, NotificationEntityType, Guid?, Guid?, CancellationToken>(
                (recipientId, _, title, body, _, _, _, _) => capturedByRecipient[recipientId] = (title, body))
            .Returns(Task.CompletedTask);

        var handler = new CancelEventCommandHandler(
            db, new TestCurrentUser(organizer.Id), time, publisher.Object);

        var result = await handler.Handle(new CancelEventCommand(@event.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue(because: string.Join("; ", result.Errors.Select(e => e.Message)));

        capturedByRecipient[turkishAttendee.Id].Body.Should().Be("\"Halı saha\" etkinliği iptal edildi.");
        capturedByRecipient[englishAttendee.Id].Body.Should().Be("\"Halı saha\" was cancelled.");
    }
}
