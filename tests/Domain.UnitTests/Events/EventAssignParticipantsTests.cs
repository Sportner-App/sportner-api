using FluentAssertions;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;
using Sportner.Domain.Events;
using Sportner.Domain.Sports;

namespace Sportner.Domain.UnitTests.Events;

public sealed class EventAssignParticipantsTests
{
    [Fact]
    public void AssignParticipants_AddsNamedGuests_AndOccupiesCapacity()
    {
        var now = new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);
        var @event = CreateEvent(now, maxParticipants: 10);

        @event.AssignParticipants(
            [
                new GuestAssignment("Ali", "Yılmaz"),
                new GuestAssignment("Veli", "Kaya")
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

    [Theory]
    [InlineData(null, "Yılmaz")]
    [InlineData("", "Yılmaz")]
    [InlineData("   ", "Yılmaz")]
    [InlineData("Ali", null)]
    [InlineData("Ali", "")]
    [InlineData("Ali", "   ")]
    public void AssignParticipants_Throws_WhenGuestNameIsIncomplete(
        string? firstName,
        string? lastName)
    {
        var now = new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);
        var @event = CreateEvent(now, maxParticipants: 10);

        var act = () => @event.AssignParticipants(
            [new GuestAssignment(firstName, lastName)],
            [],
            now);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Guest first and last name are required.");
        @event.Participants.Should().ContainSingle(participant => !participant.IsGuest);
    }

    [Fact]
    public void AssignParticipants_InvitesFriend_ThenAcceptsIntoCapacity()
    {
        var now = new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);
        var @event = CreateEvent(now, maxParticipants: 10);
        var friendId = Guid.NewGuid();
        @event.Publish(now);

        @event.AssignParticipants([], [friendId], now);

        var friend = @event.Participants.Single(participant => participant.UserId == friendId);
        friend.Kind.Should().Be(ParticipantKind.Registered);
        friend.Status.Should().Be(ParticipantStatus.Invited);
        @event.OccupiedParticipantCount().Should().Be(1);

        @event.AcceptInvitation(friendId, now.AddMinutes(1));

        friend.Status.Should().Be(ParticipantStatus.Approved);
        @event.OccupiedParticipantCount().Should().Be(2);
    }

    [Fact]
    public void DeclineInvitation_CancelsInvite_WithoutOccupyingCapacity()
    {
        var now = new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);
        var @event = CreateEvent(now, maxParticipants: 10);
        var friendId = Guid.NewGuid();
        @event.Publish(now);
        @event.AssignParticipants([], [friendId], now);

        @event.DeclineInvitation(friendId, now.AddMinutes(1));

        @event.Participants.Single(item => item.UserId == friendId).Status
            .Should().Be(ParticipantStatus.Cancelled);
        @event.OccupiedParticipantCount().Should().Be(1);
    }

    [Fact]
    public void AssignParticipants_Throws_WhenCapacityWouldBeExceeded()
    {
        var now = new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);
        var @event = CreateEvent(now, maxParticipants: 2);

        var act = () => @event.AssignParticipants(
            [new GuestAssignment("A", "One"), new GuestAssignment("B", "Two")],
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
        var assigned = @event.AssignParticipants([new GuestAssignment("Ali", "Yılmaz")], [], now);
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
