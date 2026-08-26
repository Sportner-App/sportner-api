using FluentAssertions;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;
using Sportner.Domain.Events;
using Sportner.Domain.Sports;

namespace Sportner.Domain.UnitTests.Events;

public sealed class EventAssignParticipantsTests
{
    [Fact]
    public void AssignParticipants_AddsNamedAndNamelessGuests_AndOccupiesCapacity()
    {
        var now = new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);
        var @event = CreateEvent(now, maxParticipants: 10);

        @event.AssignParticipants(
            [
                new GuestAssignment("Ali", "Yılmaz"),
                new GuestAssignment(null, null)
            ],
            [],
            now);

        @event.OccupiedParticipantCount().Should().Be(3);
        @event.Participants.Count(participant => participant.IsGuest).Should().Be(2);

        var named = @event.Participants.Single(participant => participant.GuestFirstName == "Ali");
        named.UserId.Should().BeNull();
        named.Kind.Should().Be(ParticipantKind.Guest);
        named.Status.Should().Be(ParticipantStatus.Approved);
        named.CanReview.Should().BeFalse();
    }

    [Fact]
    public void AssignParticipants_AddsFriendAsApproved_AndCountsAgainstCapacity()
    {
        var now = new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);
        var @event = CreateEvent(now, maxParticipants: 10);
        var friendId = Guid.NewGuid();

        @event.AssignParticipants([], [friendId], now);

        var friend = @event.Participants.Single(participant => participant.UserId == friendId);
        friend.Kind.Should().Be(ParticipantKind.Registered);
        friend.Status.Should().Be(ParticipantStatus.Approved);
        @event.OccupiedParticipantCount().Should().Be(2);
    }

    [Fact]
    public void AssignParticipants_Throws_WhenCapacityWouldBeExceeded()
    {
        var now = new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);
        var @event = CreateEvent(now, maxParticipants: 2);

        var act = () => @event.AssignParticipants(
            [new GuestAssignment("A", null), new GuestAssignment("B", null)],
            [],
            now);

        act.Should().Throw<DomainException>().WithMessage("Event capacity is full.");
        @event.OccupiedParticipantCount().Should().Be(1);
    }

    [Fact]
    public void RemoveAssignedParticipant_FreesGuestSlot()
    {
        var now = new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);
        var @event = CreateEvent(now, maxParticipants: 10);
        var assigned = @event.AssignParticipants([new GuestAssignment("Ali", null)], [], now);
        var guest = assigned.Single();

        @event.RemoveAssignedParticipant(guest.Id, now.AddMinutes(1));

        guest.Status.Should().Be(ParticipantStatus.Cancelled);
        @event.OccupiedParticipantCount().Should().Be(1);
    }

    private static Event CreateEvent(DateTimeOffset now, int maxParticipants)
    {
        var sport = Sport.Create("Football", 1, now, "football");

        return Event.Create(
            Guid.NewGuid(),
            sport.Id,
            "Match",
            now.AddHours(4),
            durationMinutes: 90,
            latitude: 41m,
            longitude: 29m,
            address: "Istanbul",
            now,
            maxParticipants: maxParticipants);
    }
}
