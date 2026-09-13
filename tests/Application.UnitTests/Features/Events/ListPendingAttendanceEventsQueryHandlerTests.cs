using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Sportner.Application.Abstractions.Gamification;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Features.Events.ConfirmAllAttendance;
using Sportner.Application.Features.Events.PendingAttendance;
using Sportner.Application.Features.Quests;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Sports;
using DomainEvent = Sportner.Domain.Events.Event;

namespace Sportner.Application.UnitTests.Features.Events;

public sealed class ListPendingAttendanceEventsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsCompletedEvent_WithApprovedParticipantsStillPending()
    {
        await using var db = InMemoryDb.Create();
        var now = DateTimeOffset.UtcNow;

        var organizer = TestUsers.CreateActive("+905551110090", now);
        var pendingAttendee = TestUsers.CreateActive("+905551110091", now);
        var sport = Sport.Create("Futbol", 1, now, "futbol");

        var @event = DomainEvent.Create(
            organizer.Id, sport.Id, "Halı saha", now.AddHours(-2),
            durationMinutes: 90, latitude: 41m, longitude: 29m, address: "Istanbul",
            now.AddHours(-3), maxParticipants: 10);
        @event.Publish(now.AddHours(-3));
        @event.Apply(pendingAttendee.Id, now.AddHours(-2));
        @event.ApproveParticipant(pendingAttendee.Id, now.AddHours(-2));
        @event.Complete(now);

        db.Users.AddRange(organizer, pendingAttendee);
        db.Sports.Add(sport);
        db.Events.Add(@event);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var handler = new ListPendingAttendanceEventsQueryHandler(db, new TestCurrentUser(organizer.Id));

        var result = await handler.Handle(new ListPendingAttendanceEventsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var item = result.Value!.Should().ContainSingle().Subject;
        item.EventId.Should().Be(@event.Id);
        // The organizer is auto-enrolled as an Approved participant of their own event
        // (Event.Create), so both the organizer and the explicit attendee are pending here.
        item.Participants.Should().Contain(p => p.UserId == pendingAttendee.Id);
        item.Participants.Should().Contain(p => p.UserId == organizer.Id);
    }

    [Fact]
    public async Task Handle_ExcludesEvent_OnceEveryonesAttendanceIsConfirmed()
    {
        await using var db = InMemoryDb.Create();
        var now = DateTimeOffset.UtcNow;

        var organizer = TestUsers.CreateActive("+905551110092", now);
        var attendee = TestUsers.CreateActive("+905551110093", now);
        var sport = Sport.Create("Futbol", 1, now, "futbol");

        var @event = DomainEvent.Create(
            organizer.Id, sport.Id, "Halı saha", now.AddHours(-2),
            durationMinutes: 90, latitude: 41m, longitude: 29m, address: "Istanbul",
            now.AddHours(-3), maxParticipants: 10);
        @event.Publish(now.AddHours(-3));
        @event.Apply(attendee.Id, now.AddHours(-2));
        @event.ApproveParticipant(attendee.Id, now.AddHours(-2));
        @event.Complete(now);

        db.Users.AddRange(organizer, attendee);
        db.Sports.Add(sport);
        db.Events.Add(@event);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        // Confirms EVERY still-Approved participant — including the organizer's own
        // auto-enrolled row — the way the app-launch "everyone showed up" prompt does.
        var confirmHandler = new ConfirmAllAttendanceCommandHandler(
            db,
            new TestCurrentUser(organizer.Id),
            new FakeTimeProvider(now),
            Mock.Of<IBadgeAwarder>(),
            Mock.Of<IQuestProgressTracker>(),
            Mock.Of<INotificationPublisher>());
        var confirmResult = await confirmHandler.Handle(
            new ConfirmAllAttendanceCommand(@event.Id), CancellationToken.None);
        confirmResult.IsSuccess.Should().BeTrue(
            because: string.Join("; ", confirmResult.Errors.Select(e => e.Message)));

        var handler = new ListPendingAttendanceEventsQueryHandler(db, new TestCurrentUser(organizer.Id));

        var result = await handler.Handle(new ListPendingAttendanceEventsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
