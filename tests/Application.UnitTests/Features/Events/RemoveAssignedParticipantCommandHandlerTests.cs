using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Sportner.Application.Features.Events.RemoveAssignedParticipant;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Moderation;
using Sportner.Domain.Sports;
using DomainEvent = Sportner.Domain.Events.Event;

namespace Sportner.Application.UnitTests.Features.Events;

public sealed class RemoveAssignedParticipantCommandHandlerTests
{
    [Fact]
    public async Task Handle_RemovesParticipantAndStoresSelectedReason()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(
            new DateTimeOffset(2026, 8, 28, 18, 0, 0, TimeSpan.Zero));
        var organizer = TestUsers.CreateActive("+905551111111", time.GetUtcNow());
        var participantUser = TestUsers.CreateActive("+905552222222", time.GetUtcNow());
        var sport = Sport.Create("Futbol", 1, time.GetUtcNow(), "futbol");
        var reason = ReportReason.Create(
            "HARASSMENT", "Taciz", null, 1, time.GetUtcNow());
        var @event = DomainEvent.Create(
            organizer.Id,
            sport.Id,
            "Halı saha",
            time.GetUtcNow().AddHours(3),
            90,
            41m,
            29m,
            "İstanbul",
            time.GetUtcNow(),
            maxParticipants: 10);
        @event.Publish(time.GetUtcNow());
        var (participant, _) = @event.Apply(participantUser.Id, time.GetUtcNow());
        @event.ApproveParticipant(participantUser.Id, time.GetUtcNow());

        db.Users.AddRange(organizer, participantUser);
        db.Sports.Add(sport);
        db.ReportReasons.Add(reason);
        db.Events.Add(@event);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var handler = new RemoveAssignedParticipantCommandHandler(
            db, new TestCurrentUser(organizer.Id), time);
        var result = await handler.Handle(
            new RemoveAssignedParticipantCommand(
                @event.Id, participant!.Id, reason.Id, "Tekrarlayan davranış"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await db.EventParticipants.SingleAsync(item => item.Id == participant.Id))
            .Status.Should().Be(ParticipantStatus.Cancelled);

        var audit = await db.EventParticipantRemovals.SingleAsync();
        audit.EventId.Should().Be(@event.Id);
        audit.ParticipantId.Should().Be(participant.Id);
        audit.OrganizerUserId.Should().Be(organizer.Id);
        audit.RemovedUserId.Should().Be(participantUser.Id);
        audit.ReportReasonId.Should().Be(reason.Id);
        audit.Note.Should().Be("Tekrarlayan davranış");
    }
}
