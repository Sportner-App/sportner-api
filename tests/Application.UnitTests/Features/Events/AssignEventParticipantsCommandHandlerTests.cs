using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Events;
using Sportner.Application.Features.Events.AssignEventParticipants;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Organizations;
using Sportner.Domain.Social;
using Sportner.Domain.Sports;
using Sportner.Domain.Users;
using DomainEvent = Sportner.Domain.Events.Event;

namespace Sportner.Application.UnitTests.Features.Events;

public sealed class AssignEventParticipantsCommandHandlerTests
{
    [Fact]
    public async Task Handle_AddsGuestsAndFriend_AndOccupiesCapacity()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero));

        var organizer = TestUsers.CreateActive("+905551111111", time.GetUtcNow());
        var friend = TestUsers.CreateActive("+905552222222", time.GetUtcNow());
        var friendProfile = UserProfile.Create(friend.Id, "friend-user", "Friend", time.GetUtcNow());
        friendProfile.UpdatePersonalDetails(1, new DateOnly(1995, 1, 1), time.GetUtcNow());
        friend.AttachUserProfile(friendProfile);
        var sport = Sport.Create("Futbol", 1, time.GetUtcNow(), "futbol");
        var friendship = Friendship.CreateRequest(organizer.Id, friend.Id, time.GetUtcNow());
        friendship.Accept(time.GetUtcNow());

        var @event = DomainEvent.Create(
            organizer.Id,
            sport.Id,
            "Halı saha",
            time.GetUtcNow().AddHours(4),
            durationMinutes: 90,
            latitude: 41m,
            longitude: 29m,
            address: "Istanbul",
            time.GetUtcNow(),
            maxParticipants: 10);

        db.Users.AddRange(organizer, friend);
        db.Sports.Add(sport);
        db.Friendships.Add(friendship);
        db.Events.Add(@event);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var publisher = new Mock<INotificationPublisher>();
        publisher
            .Setup(candidate => candidate.PublishAsync(
                It.IsAny<Guid>(),
                It.IsAny<NotificationType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationEntityType>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new AssignEventParticipantsCommandHandler(
            db,
            new TestCurrentUser(organizer.Id),
            time,
            publisher.Object);

        var result = await handler.Handle(
            new AssignEventParticipantsCommand(
                @event.Id,
                [new GuestAssignmentRequest("Ali", "Yılmaz", "ali@example.com"), new GuestAssignmentRequest("Veli", "Kaya", "veli@example.com")],
                [friend.Id]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(because: string.Join("; ", result.Errors.Select(error => error.Message)));
        result.Value!.OccupiedParticipantCount.Should().Be(3);

        var guests = await db.EventParticipants.AsNoTracking()
            .Where(row => row.EventId == @event.Id && row.Kind == ParticipantKind.Guest)
            .ToListAsync();

        guests.Should().HaveCount(2);
        guests.Should().OnlyContain(row => row.UserId == null && row.Status == ParticipantStatus.Approved);

        var assignedFriend = await db.EventParticipants.AsNoTracking()
            .SingleAsync(row => row.EventId == @event.Id && row.UserId == friend.Id);

        assignedFriend.Status.Should().Be(ParticipantStatus.Invited);
        assignedFriend.Kind.Should().Be(ParticipantKind.Registered);

        publisher.Verify(
            candidate => candidate.PublishAsync(
                friend.Id,
                NotificationType.EventInvitation,
                It.IsAny<string>(),
                It.IsAny<string>(),
                NotificationEntityType.Event,
                @event.Id,
                organizer.Id,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(null, "Yılmaz")]
    [InlineData("", "Yılmaz")]
    [InlineData("   ", "Yılmaz")]
    [InlineData("Ali", null)]
    [InlineData("Ali", "")]
    [InlineData("Ali", "   ")]
    public void Validator_Fails_WhenGuestNameIsIncomplete(
        string? firstName,
        string? lastName)
    {
        var validator = new AssignEventParticipantsCommandValidator();
        var command = new AssignEventParticipantsCommand(
            Guid.NewGuid(),
            [new GuestAssignmentRequest(firstName, lastName, "guest@example.com")],
            []);

        validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid-email")]
    public void Validator_Fails_WhenGuestEmailIsInvalid(string? email)
    {
        var validator = new AssignEventParticipantsCommandValidator();
        var command = new AssignEventParticipantsCommand(
            Guid.NewGuid(),
            [new GuestAssignmentRequest("Ali", "Yılmaz", email)],
            []);

        validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Fails_WhenSelectedUserIsNotAFriend()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero));

        var organizer = TestUsers.CreateActive("+905551111111", time.GetUtcNow());
        var stranger = TestUsers.CreateActive("+905552222222", time.GetUtcNow());
        var sport = Sport.Create("Futbol", 1, time.GetUtcNow(), "futbol");

        var @event = DomainEvent.Create(
            organizer.Id,
            sport.Id,
            "Halı saha",
            time.GetUtcNow().AddHours(4),
            durationMinutes: 90,
            latitude: 41m,
            longitude: 29m,
            address: "Istanbul",
            time.GetUtcNow(),
            maxParticipants: 10);

        db.Users.AddRange(organizer, stranger);
        db.Sports.Add(sport);
        db.Events.Add(@event);
        await db.SaveChangesAsync();

        var handler = new AssignEventParticipantsCommandHandler(
            db,
            new TestCurrentUser(organizer.Id),
            time,
            Mock.Of<INotificationPublisher>());

        var result = await handler.Handle(
            new AssignEventParticipantsCommand(@event.Id, [], [stranger.Id]),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Code == "Event.NotFriends");
    }

    public enum MemberState { Approved, Pending, Left, Blocked, None }

    private static async Task<(Result<EventResponse> Result, User Invitee)> RunClubInviteAsync(
        MemberState state,
        bool befriend)
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero));
        var now = time.GetUtcNow();

        var organizer = TestUsers.CreateActive("+905551111111", now);
        var invitee = TestUsers.CreateActive("+905552222222", now);
        var profile = UserProfile.Create(invitee.Id, "invitee-user", "Invitee", now);
        profile.UpdatePersonalDetails(1, new DateOnly(1995, 1, 1), now);
        invitee.AttachUserProfile(profile);
        var sport = Sport.Create("Futbol", 1, now, "futbol");
        var club = Organization.Create(organizer.Id, "Kadıköy Spor Kulübü", null, null, "ABCD2345", true, now);

        var @event = DomainEvent.Create(
            organizer.Id, sport.Id, "Kulüp etkinliği", now.AddHours(4), 90, 41m, 29m, "Istanbul", now,
            maxParticipants: 10, organizationId: club.Id, isOrganizationOnly: false);

        db.Users.AddRange(organizer, invitee);
        db.Sports.Add(sport);
        db.Organizations.Add(club);
        db.OrganizationMembers.Add(OrganizationMember.CreateFounder(club.Id, organizer.Id, now));

        if (state != MemberState.None)
        {
            var member = state == MemberState.Pending
                ? OrganizationMember.CreatePending(club.Id, invitee.Id, now)
                : OrganizationMember.CreateApproved(club.Id, invitee.Id, now);
            if (state == MemberState.Left)
            {
                member.Remove(now);
            }
            else if (state == MemberState.Blocked)
            {
                member.Block(now);
            }

            db.OrganizationMembers.Add(member);
        }

        if (befriend)
        {
            var friendship = Friendship.CreateRequest(organizer.Id, invitee.Id, now);
            friendship.Accept(now);
            db.Friendships.Add(friendship);
        }

        db.Events.Add(@event);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var handler = new AssignEventParticipantsCommandHandler(
            db,
            new TestCurrentUser(organizer.Id),
            time,
            Mock.Of<INotificationPublisher>());

        var result = await handler.Handle(
            new AssignEventParticipantsCommand(@event.Id, [], [invitee.Id]),
            CancellationToken.None);

        return (result, invitee);
    }

    [Fact]
    public async Task Handle_ClubEvent_InvitesApprovedMember_WithoutFriendship()
    {
        var (result, _) = await RunClubInviteAsync(MemberState.Approved, befriend: false);

        result.IsSuccess.Should().BeTrue(because: string.Join("; ", result.Errors.Select(error => error.Message)));
    }

    [Fact]
    public async Task Handle_ClubEvent_Fails_WhenFriendIsNotAMember()
    {
        var (result, _) = await RunClubInviteAsync(MemberState.None, befriend: true);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Code == "Event.InviteeNotClubMember");
    }

    [Theory]
    [InlineData(MemberState.Pending)]
    [InlineData(MemberState.Left)]
    [InlineData(MemberState.Blocked)]
    public async Task Handle_ClubEvent_Fails_WhenMembershipIsNotApproved(MemberState state)
    {
        var (result, _) = await RunClubInviteAsync(state, befriend: true);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Code == "Event.InviteeNotClubMember");
    }
}
