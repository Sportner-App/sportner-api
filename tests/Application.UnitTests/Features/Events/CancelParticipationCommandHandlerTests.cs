using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Sportner.Application.Features.Events.CancelParticipation;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Sports;
using DomainEvent = Sportner.Domain.Events.Event;

namespace Sportner.Application.UnitTests.Features.Events;

public sealed class CancelParticipationCommandHandlerTests
{
    [Fact]
    public async Task Handle_DecreasesEventsJoined_WhenApprovedParticipantCancels()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero));

        var organizer = TestUsers.CreateActive("+905551111111", time.GetUtcNow());
        var participant = TestUsers.CreateActive("+905552222222", time.GetUtcNow());
        var sport = Sport.Create("Tennis", 1, time.GetUtcNow(), "tennis");

        var @event = DomainEvent.Create(
            organizer.Id,
            sport.Id,
            "Court booking",
            time.GetUtcNow().AddHours(2),
            durationMinutes: 60,
            latitude: 41m,
            longitude: 29m,
            address: "Istanbul",
            time.GetUtcNow(),
            maxParticipants: 4);

        @event.Publish(time.GetUtcNow());
        @event.Apply(participant.Id, time.GetUtcNow());
        @event.ApproveParticipant(participant.Id, time.GetUtcNow());
        participant.Statistics!.IncreaseEventsJoined(time.GetUtcNow());

        db.Users.AddRange(organizer, participant);
        db.Sports.Add(sport);
        db.Events.Add(@event);
        await db.SaveChangesAsync();

        participant.Statistics.EventsJoined.Should().Be(1);

        var handler = new CancelParticipationCommandHandler(
            db,
            new TestCurrentUser(participant.Id),
            time);

        var result = await handler.Handle(
            new CancelParticipationCommand(@event.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        participant.Statistics.EventsJoined.Should().Be(0);

        var row = @event.Participants.Single(candidate => candidate.UserId == participant.Id);
        row.Status.Should().Be(ParticipantStatus.Cancelled);
    }

    [Fact]
    public async Task Handle_Fails_WhenScheduledEndHasPassed()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero));

        var organizer = TestUsers.CreateActive("+905551111111", time.GetUtcNow());
        var participant = TestUsers.CreateActive("+905552222222", time.GetUtcNow());
        var sport = Sport.Create("Tennis", 1, time.GetUtcNow(), "tennis");

        var @event = DomainEvent.Create(
            organizer.Id,
            sport.Id,
            "Court booking",
            time.GetUtcNow().AddHours(2),
            durationMinutes: 60,
            latitude: 41m,
            longitude: 29m,
            address: "Istanbul",
            time.GetUtcNow(),
            maxParticipants: 4);

        @event.Publish(time.GetUtcNow());
        @event.Apply(participant.Id, time.GetUtcNow());
        @event.ApproveParticipant(participant.Id, time.GetUtcNow());

        db.Users.AddRange(organizer, participant);
        db.Sports.Add(sport);
        db.Events.Add(@event);
        await db.SaveChangesAsync();

        time.Advance(TimeSpan.FromHours(4));

        var handler = new CancelParticipationCommandHandler(
            db,
            new TestCurrentUser(participant.Id),
            time);

        var result = await handler.Handle(
            new CancelParticipationCommand(@event.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Code == "Event.ParticipationLocked");
        @event.Participants.Single(row => row.UserId == participant.Id)
            .Status.Should().Be(ParticipantStatus.Approved);
    }
}
