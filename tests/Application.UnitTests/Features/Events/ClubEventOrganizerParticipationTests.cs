using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Sportner.Application.Features.Events;
using Sportner.Application.Features.Events.CreateEvent;
using Sportner.Application.Features.Events.PendingJoinRequests;
using Sportner.Application.Features.Reviews;
using Sportner.Application.Features.Reviews.ListReviewablePeers;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Organizations;
using Sportner.Domain.Sports;
using DomainEvent = Sportner.Domain.Events.Event;

namespace Sportner.Application.UnitTests.Features.Events;

public sealed class ClubEventOrganizerParticipationTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    private static DomainEvent CompletedEvent(
        Guid creatorId,
        Guid sportId,
        Guid attendeeId,
        Guid? clubId,
        bool organizerParticipates)
    {
        var created = Now.AddHours(-4);
        var @event = DomainEvent.Create(
            creatorId, sportId, "Halı saha", Now.AddHours(-3), 90, 41m, 29m, "İstanbul", created,
            maxParticipants: 10,
            organizationId: clubId,
            isOrganizationOnly: false,
            organizerParticipates: organizerParticipates);
        @event.Publish(created);
        @event.Apply(attendeeId, created);
        @event.ApproveParticipant(attendeeId, created);
        @event.Complete(Now);
        @event.ConfirmAttendance(attendeeId, Now);
        return @event;
    }

    // --- Değerlendirme uygunluğu ---------------------------------------------------------------------

    [Fact]
    public void ReviewEligibility_NonParticipatingCreator_NeitherReviewsNorIsReviewed()
    {
        var creator = Guid.NewGuid();
        var attendee = Guid.NewGuid();
        var @event = CompletedEvent(creator, Guid.NewGuid(), attendee, Guid.NewGuid(), organizerParticipates: false);
        var attendeeRow = @event.Participants.Single(p => p.UserId == attendee);

        ReviewEligibility.CanReviewEvent(@event, creator, null).Should().BeFalse();
        ReviewEligibility.CanBeReviewed(@event, creator, null).Should().BeFalse();
        // Katılımcılar birbirini yine değerlendirebilir.
        ReviewEligibility.CanBeReviewed(@event, attendee, attendeeRow).Should().BeTrue();
    }

    [Fact]
    public void ReviewEligibility_ParticipatingCreator_KeepsSpecialCase()
    {
        var creator = Guid.NewGuid();
        var @event = CompletedEvent(creator, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), organizerParticipates: true);

        ReviewEligibility.CanReviewEvent(@event, creator, null).Should().BeTrue();
        ReviewEligibility.CanBeReviewed(@event, creator, null).Should().BeTrue();
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public async Task ListReviewablePeers_ListsCreatorOnlyWhenCreatorPlayed(bool participates, int expectedOrganizerRows)
    {
        await using var db = InMemoryDb.Create();
        var creator = TestUsers.CreateActive("+905551111111", Now);
        var attendee = TestUsers.CreateActive("+905552222222", Now);
        var sport = Sport.Create("Futbol", 1, Now, "futbol");
        var club = Organization.Create(creator.Id, "Kulüp", null, null, "ABCD2345", true, Now);
        var @event = CompletedEvent(creator.Id, sport.Id, attendee.Id, club.Id, participates);

        db.Users.AddRange(creator, attendee);
        db.Sports.Add(sport);
        db.Organizations.Add(club);
        db.Events.Add(@event);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var handler = new ListReviewablePeersQueryHandler(db, new TestCurrentUser(attendee.Id));
        var result = await handler.Handle(new ListReviewablePeersQuery(@event.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Count(peer => peer.UserId == creator.Id).Should().Be(expectedOrganizerRows);
    }

    // --- Oluşturma ----------------------------------------------------------------------------

    [Fact]
    public void Validator_RejectsOptOut_WithoutClub_AndAcceptsItWithClub()
    {
        var validator = new CreateEventCommandValidator();
        var command = new CreateEventCommand(
            Guid.NewGuid(), "Başlık", null, DateTimeOffset.UtcNow.AddDays(2), 90, 41m, 29m, "İstanbul",
            6, null, null);

        validator.Validate(command with { OrganizerParticipates = false }).IsValid.Should().BeFalse();
        validator.Validate(command with { OrganizerParticipates = false, OrganizationId = Guid.NewGuid() })
            .IsValid.Should().BeTrue();
        validator.Validate(command with { OrganizerParticipates = true }).IsValid.Should().BeTrue();
        validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CreateEvent_OptOutForClubEvent_LeavesCapacityEmpty_AndReturnsFlag()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(Now);
        var founder = TestUsers.CreateActive("+905551111111", Now);
        var sport = Sport.Create("Futbol", 1, Now, "futbol");
        var club = Organization.Create(founder.Id, "Kulüp", null, null, "ABCD2345", true, Now);
        db.Users.Add(founder);
        db.Sports.Add(sport);
        db.Organizations.Add(club);
        db.OrganizationMembers.Add(OrganizationMember.CreateFounder(club.Id, founder.Id, Now));
        await db.SaveChangesAsync();

        var handler = new CreateEventCommandHandler(db, new TestCurrentUser(founder.Id), time);
        var result = await handler.Handle(
            new CreateEventCommand(
                sport.Id, "Kulüp maçı", null, Now.AddDays(2), 90, 41m, 29m, "İstanbul", 4, null, null,
                OrganizationId: club.Id,
                OrganizerParticipates: false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsOrganizerParticipant.Should().BeFalse();
        result.Value.OccupiedParticipantCount.Should().Be(0);
        result.Value.OrganizationName.Should().Be("Kulüp");
        (await db.EventParticipants.CountAsync()).Should().Be(0);
    }

    // --- Bekleyen başvuru listesi ---------------------------------------------------------------

    [Fact]
    public async Task ListPendingJoinRequestEvents_IncludesClubEventsTheCallerManages()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(Now);
        var founder = TestUsers.CreateActive("+905551111111", Now);
        var admin = TestUsers.CreateActive("+905552222222", Now);
        var member = TestUsers.CreateActive("+905553333333", Now);
        var applicant = TestUsers.CreateActive("+905554444444", Now);
        var sport = Sport.Create("Futbol", 1, Now, "futbol");
        var club = Organization.Create(founder.Id, "Kulüp", null, null, "ABCD2345", true, Now);
        var adminMembership = OrganizationMember.CreateApproved(club.Id, admin.Id, Now);
        adminMembership.SetRole(OrganizationRole.Admin, Now);

        var @event = DomainEvent.Create(
            founder.Id, sport.Id, "Kulüp maçı", Now.AddDays(1), 90, 41m, 29m, "İstanbul", Now,
            maxParticipants: 10, organizationId: club.Id, isOrganizationOnly: false);
        @event.Publish(Now);
        @event.Apply(applicant.Id, Now);

        db.Users.AddRange(founder, admin, member, applicant);
        db.Sports.Add(sport);
        db.Organizations.Add(club);
        db.OrganizationMembers.AddRange(
            OrganizationMember.CreateFounder(club.Id, founder.Id, Now),
            adminMembership,
            OrganizationMember.CreateApproved(club.Id, member.Id, Now));
        db.Events.Add(@event);
        await db.SaveChangesAsync();

        async Task<int> CountFor(Guid userId)
        {
            var handler = new ListPendingJoinRequestEventsQueryHandler(db, new TestCurrentUser(userId), time);
            var result = await handler.Handle(new ListPendingJoinRequestEventsQuery(), CancellationToken.None);
            return result.Value!.Count;
        }

        (await CountFor(founder.Id)).Should().Be(1);
        (await CountFor(admin.Id)).Should().Be(1);
        (await CountFor(member.Id)).Should().Be(0);
    }
}
