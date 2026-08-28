using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Sportner.Application.Features.Events.AcceptEventInvitation;
using Sportner.Application.Features.Events.DeclineEventInvitation;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Messaging;
using Sportner.Domain.Sports;
using DomainEvent = Sportner.Domain.Events.Event;

namespace Sportner.Application.UnitTests.Features.Events;

public sealed class EventInvitationCommandHandlerTests
{
    [Fact]
    public async Task Accept_AddsInviteeToRosterAndConversation()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 28, 12, 0, 0, TimeSpan.Zero));
        var organizer = TestUsers.CreateActive("+905551111111", time.GetUtcNow());
        var invitee = TestUsers.CreateActive("+905552222222", time.GetUtcNow());
        var sport = Sport.Create("Futbol", 1, time.GetUtcNow(), "futbol");
        var @event = CreateEvent(organizer.Id, sport.Id, time);
        @event.AssignParticipants([], [invitee.Id], time.GetUtcNow());

        db.Users.AddRange(organizer, invitee);
        db.Sports.Add(sport);
        db.Events.Add(@event);
        db.Conversations.Add(Conversation.CreateEventConversation(
            @event.Id, organizer.Id, time.GetUtcNow(), @event.Title));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var handler = new AcceptEventInvitationCommandHandler(
            db, new TestCurrentUser(invitee.Id), time);
        var result = await handler.Handle(
            new AcceptEventInvitationCommand(@event.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OccupiedParticipantCount.Should().Be(2);
        (await db.EventParticipants.SingleAsync(item => item.UserId == invitee.Id)).Status
            .Should().Be(ParticipantStatus.Approved);
        (await db.ConversationMembers.SingleAsync(item => item.UserId == invitee.Id)).LeftAt
            .Should().BeNull();
    }

    [Fact]
    public async Task Decline_CancelsInvitation()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 28, 12, 0, 0, TimeSpan.Zero));
        var organizer = TestUsers.CreateActive("+905551111111", time.GetUtcNow());
        var invitee = TestUsers.CreateActive("+905552222222", time.GetUtcNow());
        var sport = Sport.Create("Futbol", 1, time.GetUtcNow(), "futbol");
        var @event = CreateEvent(organizer.Id, sport.Id, time);
        @event.AssignParticipants([], [invitee.Id], time.GetUtcNow());
        db.Users.AddRange(organizer, invitee);
        db.Sports.Add(sport);
        db.Events.Add(@event);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var handler = new DeclineEventInvitationCommandHandler(
            db, new TestCurrentUser(invitee.Id), time);
        var result = await handler.Handle(
            new DeclineEventInvitationCommand(@event.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await db.EventParticipants.SingleAsync(item => item.UserId == invitee.Id)).Status
            .Should().Be(ParticipantStatus.Cancelled);
    }

    private static DomainEvent CreateEvent(
        Guid organizerId,
        Guid sportId,
        FakeTimeProvider time)
    {
        var @event = DomainEvent.Create(
            organizerId,
            sportId,
            "Halı saha",
            time.GetUtcNow().AddHours(4),
            durationMinutes: 90,
            latitude: 41m,
            longitude: 29m,
            address: "Istanbul",
            time.GetUtcNow(),
            maxParticipants: 10);
        @event.Publish(time.GetUtcNow());
        return @event;
    }
}
