using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Sportner.Application.Abstractions.Gamification;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Features.Events.ConfirmAllAttendance;
using Sportner.Application.Features.Quests;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Sports;
using DomainEvent = Sportner.Domain.Events.Event;

namespace Sportner.Application.UnitTests.Features.Events;

public sealed class ConfirmAllAttendanceCommandHandlerTests
{
    [Fact]
    public async Task Handle_ConfirmsEveryone_WhenNoAbsentUsersGiven()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 24, 5, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var organizer = TestUsers.CreateActive("+905551110060", now);
        var attendeeA = TestUsers.CreateActive("+905551110061", now);
        var attendeeB = TestUsers.CreateActive("+905551110062", now);
        var sport = Sport.Create("Futbol", 1, now, "futbol");

        var @event = DomainEvent.Create(
            organizer.Id, sport.Id, "Halı saha", now.AddHours(-2),
            durationMinutes: 90, latitude: 41m, longitude: 29m, address: "Istanbul",
            now.AddHours(-3), maxParticipants: 10);
        @event.Publish(now.AddHours(-3));
        @event.Apply(attendeeA.Id, now.AddHours(-2));
        @event.ApproveParticipant(attendeeA.Id, now.AddHours(-2));
        @event.Apply(attendeeB.Id, now.AddHours(-2));
        @event.ApproveParticipant(attendeeB.Id, now.AddHours(-2));
        @event.Complete(now);

        db.Users.AddRange(organizer, attendeeA, attendeeB);
        db.Sports.Add(sport);
        db.Events.Add(@event);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var handler = new ConfirmAllAttendanceCommandHandler(
            db,
            new TestCurrentUser(organizer.Id),
            time,
            Mock.Of<IBadgeAwarder>(),
            Mock.Of<IQuestProgressTracker>(),
            Mock.Of<INotificationPublisher>());

        var result = await handler.Handle(
            new ConfirmAllAttendanceCommand(@event.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue(because: string.Join("; ", result.Errors.Select(e => e.Message)));

        var reloaded = await db.Events.FindAsync(@event.Id);
        reloaded!.Participants.Should().OnlyContain(p => p.Status == ParticipantStatus.Attended);
    }

    [Fact]
    public async Task Handle_MarksListedUsersAsNoShow_AndRestAsAttended()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 24, 5, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var organizer = TestUsers.CreateActive("+905551110070", now);
        var attended = TestUsers.CreateActive("+905551110071", now);
        var absent = TestUsers.CreateActive("+905551110072", now);
        var sport = Sport.Create("Futbol", 1, now, "futbol");

        var @event = DomainEvent.Create(
            organizer.Id, sport.Id, "Halı saha", now.AddHours(-2),
            durationMinutes: 90, latitude: 41m, longitude: 29m, address: "Istanbul",
            now.AddHours(-3), maxParticipants: 10);
        @event.Publish(now.AddHours(-3));
        @event.Apply(attended.Id, now.AddHours(-2));
        @event.ApproveParticipant(attended.Id, now.AddHours(-2));
        @event.Apply(absent.Id, now.AddHours(-2));
        @event.ApproveParticipant(absent.Id, now.AddHours(-2));
        @event.Complete(now);

        db.Users.AddRange(organizer, attended, absent);
        db.Sports.Add(sport);
        db.Events.Add(@event);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var handler = new ConfirmAllAttendanceCommandHandler(
            db,
            new TestCurrentUser(organizer.Id),
            time,
            Mock.Of<IBadgeAwarder>(),
            Mock.Of<IQuestProgressTracker>(),
            Mock.Of<INotificationPublisher>());

        var result = await handler.Handle(
            new ConfirmAllAttendanceCommand(@event.Id, [absent.Id]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue(because: string.Join("; ", result.Errors.Select(e => e.Message)));

        var reloaded = await db.Events.FindAsync(@event.Id);
        reloaded!.Participants.Single(p => p.UserId == attended.Id).Status
            .Should().Be(ParticipantStatus.Attended);
        reloaded.Participants.Single(p => p.UserId == absent.Id).Status
            .Should().Be(ParticipantStatus.NoShow);
    }

    [Fact]
    public async Task Handle_Fails_WhenEventNotYetCompleted()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 24, 5, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var organizer = TestUsers.CreateActive("+905551110080", now);
        var sport = Sport.Create("Futbol", 1, now, "futbol");

        var @event = DomainEvent.Create(
            organizer.Id, sport.Id, "Halı saha", now.AddHours(2),
            durationMinutes: 90, latitude: 41m, longitude: 29m, address: "Istanbul",
            now, maxParticipants: 10);
        @event.Publish(now);

        db.Users.Add(organizer);
        db.Sports.Add(sport);
        db.Events.Add(@event);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var handler = new ConfirmAllAttendanceCommandHandler(
            db,
            new TestCurrentUser(organizer.Id),
            time,
            Mock.Of<IBadgeAwarder>(),
            Mock.Of<IQuestProgressTracker>(),
            Mock.Of<INotificationPublisher>());

        var result = await handler.Handle(
            new ConfirmAllAttendanceCommand(@event.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(error => error.Code == "Event.NotCompleted");
    }
}
