using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Sportner.Application.Features.Events.ApplyToEvent;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Sports;
using DomainEvent = Sportner.Domain.Events.Event;

namespace Sportner.Application.UnitTests.Features.Events;

public sealed class ApplyToEventCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesPendingParticipant_ForPublishedEvent()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero));

        Guid eventId;
        Guid applicantId;

        await using (var seed = InMemoryDb.Create(databaseName))
        {
            var organizer = TestUsers.CreateActive("+905551111111", time.GetUtcNow());
            var applicant = TestUsers.CreateActive("+905552222222", time.GetUtcNow());
            var sport = Sport.Create("Football", 1, time.GetUtcNow(), "football");

            var @event = DomainEvent.Create(
                organizer.Id,
                sport.Id,
                "Match",
                time.GetUtcNow().AddHours(2),
                durationMinutes: 90,
                latitude: 41m,
                longitude: 29m,
                address: "Istanbul",
                time.GetUtcNow(),
                maxParticipants: 8);

            @event.Publish(time.GetUtcNow());

            seed.Users.AddRange(organizer, applicant);
            seed.Sports.Add(sport);
            seed.Events.Add(@event);
            await seed.SaveChangesAsync();

            eventId = @event.Id;
            applicantId = applicant.Id;
        }

        await using var db = InMemoryDb.Create(databaseName);
        var handler = new ApplyToEventCommandHandler(
            db,
            new TestCurrentUser(applicantId),
            time);

        var result = await handler.Handle(new ApplyToEventCommand(eventId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.JoinedWaitlist.Should().BeFalse();
        result.Value.ParticipantStatus.Should().Be((short)ParticipantStatus.Pending);

        var loadedEvent = await db.Events
            .Include(candidate => candidate.Participants)
            .SingleAsync(candidate => candidate.Id == eventId);
        loadedEvent.OccupiedParticipantCount().Should().Be(1);
    }

    [Fact]
    public async Task Handle_Fails_WhenAlreadyApplied()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero));

        Guid eventId;
        Guid applicantId;

        await using (var seed = InMemoryDb.Create(databaseName))
        {
            var organizer = TestUsers.CreateActive("+905551111111", time.GetUtcNow());
            var applicant = TestUsers.CreateActive("+905552222222", time.GetUtcNow());
            var sport = Sport.Create("Football", 1, time.GetUtcNow(), "football");

            var @event = DomainEvent.Create(
                organizer.Id,
                sport.Id,
                "Match",
                time.GetUtcNow().AddHours(2),
                durationMinutes: 90,
                latitude: 41m,
                longitude: 29m,
                address: "Istanbul",
                time.GetUtcNow(),
                maxParticipants: 8);

            @event.Publish(time.GetUtcNow());
            @event.Apply(applicant.Id, time.GetUtcNow());

            seed.Users.AddRange(organizer, applicant);
            seed.Sports.Add(sport);
            seed.Events.Add(@event);
            await seed.SaveChangesAsync();

            eventId = @event.Id;
            applicantId = applicant.Id;
        }

        await using var db = InMemoryDb.Create(databaseName);
        var handler = new ApplyToEventCommandHandler(
            db,
            new TestCurrentUser(applicantId),
            time);

        var result = await handler.Handle(new ApplyToEventCommand(eventId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Code == "Event.AlreadyApplied");
    }

    [Fact]
    public async Task Handle_ReopensCancelledParticipant_AsPending()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero));

        Guid eventId;
        Guid applicantId;

        await using (var seed = InMemoryDb.Create(databaseName))
        {
            var organizer = TestUsers.CreateActive("+905551111111", time.GetUtcNow());
            var applicant = TestUsers.CreateActive("+905552222222", time.GetUtcNow());
            var sport = Sport.Create("Football", 1, time.GetUtcNow(), "football");

            var @event = DomainEvent.Create(
                organizer.Id,
                sport.Id,
                "Match",
                time.GetUtcNow().AddHours(2),
                durationMinutes: 90,
                latitude: 41m,
                longitude: 29m,
                address: "Istanbul",
                time.GetUtcNow(),
                maxParticipants: 8);

            @event.Publish(time.GetUtcNow());
            @event.Apply(applicant.Id, time.GetUtcNow());
            @event.ApproveParticipant(applicant.Id, time.GetUtcNow());
            @event.CancelParticipation(applicant.Id, time.GetUtcNow());

            seed.Users.AddRange(organizer, applicant);
            seed.Sports.Add(sport);
            seed.Events.Add(@event);
            await seed.SaveChangesAsync();

            eventId = @event.Id;
            applicantId = applicant.Id;
        }

        await using var db = InMemoryDb.Create(databaseName);
        var handler = new ApplyToEventCommandHandler(
            db,
            new TestCurrentUser(applicantId),
            time);

        var result = await handler.Handle(new ApplyToEventCommand(eventId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue(because: string.Join("; ", result.Errors.Select(e => e.Message)));
        result.Value!.JoinedWaitlist.Should().BeFalse();
        result.Value.ParticipantStatus.Should().Be((short)ParticipantStatus.Pending);
    }
}
