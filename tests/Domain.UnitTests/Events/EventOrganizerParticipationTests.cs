using FluentAssertions;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;
using Sportner.Domain.Events;

namespace Sportner.Domain.UnitTests.Events;

public sealed class EventOrganizerParticipationTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);

    private static Event CreateEvent(
        bool organizerParticipates = true,
        Guid? organizationId = null,
        int? maxParticipants = 2,
        Guid? organizerId = null)
    {
        var @event = Event.Create(
            organizerId ?? Guid.NewGuid(),
            Guid.NewGuid(),
            "Halı saha",
            Now.AddDays(1),
            90,
            41m,
            29m,
            "İstanbul",
            Now,
            maxParticipants: maxParticipants,
            organizationId: organizationId,
            isOrganizationOnly: organizationId is not null,
            organizerParticipates: organizerParticipates);
        @event.Publish(Now);
        return @event;
    }

    [Fact]
    public void Create_ByDefault_AddsOrganizerAsApprovedParticipant()
    {
        var @event = CreateEvent(organizationId: Guid.NewGuid());

        @event.IsOrganizerParticipant.Should().BeTrue();
        @event.OccupiedParticipantCount().Should().Be(1);
    }

    [Fact]
    public void Create_OptOut_ForClubEvent_AddsNoOrganizerRow_AndKeepsCapacityFree()
    {
        var @event = CreateEvent(organizerParticipates: false, organizationId: Guid.NewGuid(), maxParticipants: 2);

        @event.IsOrganizerParticipant.Should().BeFalse();
        @event.Participants.Should().BeEmpty();
        @event.OccupiedParticipantCount().Should().Be(0);
        @event.HasAvailableCapacity().Should().BeTrue();
        @event.Status.Should().Be(EventStatus.Published);
    }

    [Fact]
    public void Create_OptOut_WithoutClub_Throws()
    {
        var act = () => CreateEvent(organizerParticipates: false, organizationId: null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void SetOrganizerParticipation_Join_WhenFull_Throws()
    {
        var @event = CreateEvent(organizerParticipates: false, organizationId: Guid.NewGuid(), maxParticipants: 1);
        @event.AssignParticipants([new GuestAssignment("Ali", "Yılmaz", "ali@example.com")], [], Now);
        @event.Status.Should().Be(EventStatus.Full);

        var act = () => @event.SetOrganizerParticipation(true, Now);

        act.Should().Throw<DomainException>();
        @event.IsOrganizerParticipant.Should().BeFalse();
    }

    [Fact]
    public void SetOrganizerParticipation_Join_AddsApprovedOrganizerRow_AndFillsCapacity()
    {
        var @event = CreateEvent(organizerParticipates: false, organizationId: Guid.NewGuid(), maxParticipants: 1);

        var added = @event.SetOrganizerParticipation(true, Now);

        added.Should().NotBeNull();
        added!.UserId.Should().Be(@event.OrganizerUserId);
        added.Status.Should().Be(ParticipantStatus.Approved);
        @event.IsOrganizerParticipant.Should().BeTrue();
        @event.Status.Should().Be(EventStatus.Full);
    }

    [Fact]
    public void SetOrganizerParticipation_Leave_FreesSlot_WithoutPromotingWaitlist()
    {
        var @event = CreateEvent(organizationId: Guid.NewGuid(), maxParticipants: 1);
        var waiting = Guid.NewGuid();
        @event.Apply(waiting, Now);
        @event.Status.Should().Be(EventStatus.Full);
        @event.Waitlist.Should().ContainSingle();

        @event.SetOrganizerParticipation(false, Now);

        @event.IsOrganizerParticipant.Should().BeFalse();
        @event.OccupiedParticipantCount().Should().Be(0);
        @event.Status.Should().Be(EventStatus.Published);
        // Normal katılımcı iptali gibi: terfi organizatör/yönetici elinde.
        @event.Waitlist.Should().ContainSingle(entry => entry.UserId == waiting);
        @event.Participants.Single(p => p.UserId == @event.OrganizerUserId)
            .Status.Should().Be(ParticipantStatus.Cancelled);
    }

    [Fact]
    public void SetOrganizerParticipation_Rejoin_ReactivatesExistingRow()
    {
        var @event = CreateEvent(organizationId: Guid.NewGuid(), maxParticipants: 3);
        @event.SetOrganizerParticipation(false, Now);

        var added = @event.SetOrganizerParticipation(true, Now.AddMinutes(1));

        added.Should().BeNull("mevcut satır yeniden etkinleştirilir");
        @event.Participants.Count(p => p.UserId == @event.OrganizerUserId).Should().Be(1);
        @event.Participants.Single(p => p.UserId == @event.OrganizerUserId)
            .Status.Should().Be(ParticipantStatus.Approved);
        @event.IsOrganizerParticipant.Should().BeTrue();
    }

    [Fact]
    public void SetOrganizerParticipation_IsNoOp_WhenAlreadyInRequestedState()
    {
        var @event = CreateEvent(organizationId: Guid.NewGuid());

        @event.SetOrganizerParticipation(true, Now).Should().BeNull();
        @event.OccupiedParticipantCount().Should().Be(1);
    }

    [Fact]
    public void SetOrganizerParticipation_ForNonClubEvent_Throws()
    {
        var @event = CreateEvent(organizationId: null);

        var act = () => @event.SetOrganizerParticipation(false, Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void SetOrganizerParticipation_AfterEventStarted_Throws()
    {
        var @event = CreateEvent(organizationId: Guid.NewGuid());

        var act = () => @event.SetOrganizerParticipation(false, Now.AddDays(2));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void SetOrganizerParticipation_WhenCancelled_Throws()
    {
        var @event = CreateEvent(organizationId: Guid.NewGuid());
        @event.Cancel(Now);

        var act = () => @event.SetOrganizerParticipation(false, Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void CancelParticipation_StillRejectsOrganizer()
    {
        var @event = CreateEvent(organizationId: Guid.NewGuid());

        var act = () => @event.CancelParticipation(@event.OrganizerUserId, Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void NextSeriesOccurrence_KeepsOrganizerParticipationChoice()
    {
        var @event = CreateEvent(organizerParticipates: false, organizationId: Guid.NewGuid(), maxParticipants: 4);
        @event.StartSeries(1, 3, Now);

        var next = @event.CreateNextSeriesOccurrence(Now.AddDays(3));

        next.IsOrganizerParticipant.Should().BeFalse();
        next.Participants.Should().BeEmpty();
    }
}
