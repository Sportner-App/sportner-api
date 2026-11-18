using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Features.Events.CreateEvent;
using Sportner.Application.Features.Organizations.DeleteOrganization;
using Sportner.Application.Features.Organizations.GetOrganizationById;
using Sportner.Application.Features.Organizations.ListMyOrganizations;
using Sportner.Application.Features.Organizations.RotateInviteCode;
using Sportner.Application.Features.Organizations.TransferOrganizationFounder;
using Sportner.Application.Features.Organizations.UpdateOrganization;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Messaging;
using Sportner.Domain.Organizations;
using Sportner.Domain.Sports;
using Sportner.Domain.Users;
using Sportner.Infrastructure.Persistence;
using DomainEvent = Sportner.Domain.Events.Event;

namespace Sportner.Application.UnitTests.Organizations;

public sealed class OrganizationLifecycleTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    private sealed class Scenario
    {
        public required AppDbContext Db { get; init; }
        public required FakeTimeProvider Time { get; init; }
        public required User Founder { get; init; }
        public required User Admin { get; init; }
        public required User Member { get; init; }
        public required User Outsider { get; init; }
        public required Organization Club { get; init; }
        public required Sport Sport { get; init; }
    }

    private static async Task<Scenario> CreateScenarioAsync()
    {
        var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(Now);
        var founder = TestUsers.CreateActive("+905551111111", Now);
        var admin = TestUsers.CreateActive("+905552222222", Now);
        var member = TestUsers.CreateActive("+905553333333", Now);
        var outsider = TestUsers.CreateActive("+905554444444", Now);
        var sport = Sport.Create("Futbol", 1, Now, "futbol");
        var club = Organization.Create(founder.Id, "Kulüp", null, null, "ABCD2345", true, Now);

        var adminMembership = OrganizationMember.CreateApproved(club.Id, admin.Id, Now);
        adminMembership.SetRole(OrganizationRole.Admin, Now);

        db.Users.AddRange(founder, admin, member, outsider);
        db.Sports.Add(sport);
        db.Organizations.Add(club);
        db.OrganizationMembers.AddRange(
            OrganizationMember.CreateFounder(club.Id, founder.Id, Now),
            adminMembership,
            OrganizationMember.CreateApproved(club.Id, member.Id, Now));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        return new Scenario
        {
            Db = db,
            Time = time,
            Founder = founder,
            Admin = admin,
            Member = member,
            Outsider = outsider,
            Club = club,
            Sport = sport
        };
    }

    private static DomainEvent NewClubEvent(Scenario s, string title, bool publish = true)
    {
        var @event = DomainEvent.Create(
            s.Founder.Id,
            s.Sport.Id,
            title,
            Now.AddDays(1),
            90,
            41m,
            29m,
            "İstanbul",
            Now,
            maxParticipants: 10,
            organizationId: s.Club.Id,
            isOrganizationOnly: false);

        if (publish)
        {
            @event.Publish(Now);
        }

        return @event;
    }

    private static TransferOrganizationFounderCommandHandler TransferHandler(
        Scenario s, Guid actorId, Mock<INotificationPublisher>? publisher = null) =>
        new(s.Db, new TestCurrentUser(actorId), s.Time, (publisher ?? new Mock<INotificationPublisher>()).Object);

    // ---- Transfer ----

    [Fact]
    public async Task Transfer_ByFounder_SwapsRolesAndNotifiesNewFounder()
    {
        var s = await CreateScenarioAsync();
        var publisher = new Mock<INotificationPublisher>();

        var result = await TransferHandler(s, s.Founder.Id, publisher).Handle(
            new TransferOrganizationFounderCommand(s.Club.Id, s.Member.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.MyRole.Should().Be((short)OrganizationRole.Admin);
        result.Value.FounderUserId.Should().Be(s.Member.Id);
        result.Value.CanDelete.Should().BeFalse();
        result.Value.CanTransferFounder.Should().BeFalse();

        s.Db.ChangeTracker.Clear();
        var members = await s.Db.OrganizationMembers.ToListAsync();
        members.Single(m => m.UserId == s.Member.Id).Role.Should().Be(OrganizationRole.Founder);
        members.Single(m => m.UserId == s.Founder.Id).Role.Should().Be(OrganizationRole.Admin);
        (await s.Db.Organizations.SingleAsync()).FounderUserId.Should().Be(s.Member.Id);

        publisher.Verify(
            p => p.PublishAsync(
                s.Member.Id,
                NotificationType.OrganizationRoleChanged,
                It.IsAny<string>(),
                It.IsAny<string>(),
                NotificationEntityType.Organization,
                s.Club.Id,
                s.Founder.Id,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Transfer_ByNonFounder_ReturnsNotFounder()
    {
        var s = await CreateScenarioAsync();

        var result = await TransferHandler(s, s.Admin.Id).Handle(
            new TransferOrganizationFounderCommand(s.Club.Id, s.Member.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "Organization.NotFounder");
    }

    [Fact]
    public async Task Transfer_ToNonMember_ReturnsTargetNotApproved()
    {
        var s = await CreateScenarioAsync();

        var result = await TransferHandler(s, s.Founder.Id).Handle(
            new TransferOrganizationFounderCommand(s.Club.Id, s.Outsider.Id), CancellationToken.None);

        result.Errors.Should().ContainSingle(e => e.Code == "Organization.TransferTargetNotApproved");
    }

    [Fact]
    public async Task Transfer_ToPendingMember_ReturnsTargetNotApproved()
    {
        var s = await CreateScenarioAsync();
        s.Db.OrganizationMembers.Add(OrganizationMember.CreatePending(s.Club.Id, s.Outsider.Id, Now));
        await s.Db.SaveChangesAsync();

        var result = await TransferHandler(s, s.Founder.Id).Handle(
            new TransferOrganizationFounderCommand(s.Club.Id, s.Outsider.Id), CancellationToken.None);

        result.Errors.Should().ContainSingle(e => e.Code == "Organization.TransferTargetNotApproved");
    }

    [Fact]
    public async Task Transfer_ToSelf_ReturnsCannotTransferToSelf()
    {
        var s = await CreateScenarioAsync();

        var result = await TransferHandler(s, s.Founder.Id).Handle(
            new TransferOrganizationFounderCommand(s.Club.Id, s.Founder.Id), CancellationToken.None);

        result.Errors.Should().ContainSingle(e => e.Code == "Organization.CannotTransferToSelf");
    }

    // ---- Delete ----

    private static DeleteOrganizationCommandHandler DeleteHandler(
        Scenario s, Guid actorId, Mock<INotificationPublisher>? publisher = null) =>
        new(s.Db, new TestCurrentUser(actorId), s.Time, (publisher ?? new Mock<INotificationPublisher>()).Object);

    [Fact]
    public async Task Delete_ByFounder_CancelsUpcomingEvents_KeepsCompleted_AndHidesClub()
    {
        var s = await CreateScenarioAsync();
        var upcoming = NewClubEvent(s, "Yaklaşan");
        upcoming.Apply(s.Member.Id, Now);
        var draft = NewClubEvent(s, "Taslak", publish: false);
        var started = DomainEvent.Create(
            s.Founder.Id, s.Sport.Id, "Dün", Now.AddDays(-2), 90, 41m, 29m, "İstanbul",
            Now.AddDays(-3), maxParticipants: 10, organizationId: s.Club.Id, isOrganizationOnly: false);
        started.Publish(Now.AddDays(-3));
        started.Apply(s.Member.Id, Now.AddDays(-3));
        s.Db.Events.Add(started);
        var completed = NewClubEvent(s, "Biten");
        completed.Complete(Now.AddDays(2));
        s.Db.Events.AddRange(upcoming, draft, completed);
        s.Db.Conversations.Add(
            Conversation.CreateEventConversation(upcoming.Id, s.Founder.Id, Now, upcoming.Title));
        await s.Db.SaveChangesAsync();
        s.Db.ChangeTracker.Clear();

        var publisher = new Mock<INotificationPublisher>();
        var result = await DeleteHandler(s, s.Founder.Id, publisher).Handle(
            new DeleteOrganizationCommand(s.Club.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        s.Db.ChangeTracker.Clear();

        var events = await s.Db.Events.ToListAsync();
        events.Single(e => e.Id == upcoming.Id).Status.Should().Be(EventStatus.Cancelled);
        events.Single(e => e.Id == draft.Id).Status.Should().Be(EventStatus.Cancelled);
        events.Single(e => e.Id == completed.Id).Status.Should().Be(EventStatus.Completed);
        events.Single(e => e.Id == started.Id).Status.Should().Be(EventStatus.Published);
        (await s.Db.Conversations.SingleAsync()).IsClosed.Should().BeTrue();

        publisher.Verify(
            p => p.PublishAsync(
                s.Member.Id,
                NotificationType.EventCancelled,
                It.IsAny<string>(),
                It.IsAny<string>(),
                NotificationEntityType.Event,
                upcoming.Id,
                s.Founder.Id,
                It.IsAny<CancellationToken>()),
            Times.Once);

        (await s.Db.Organizations.CountAsync()).Should().Be(0);
        (await s.Db.Organizations.IgnoreQueryFilters().SingleAsync()).DeletedAt.Should().NotBeNull();
        (await s.Db.OrganizationMembers.CountAsync()).Should().Be(3);

        var get = await new GetOrganizationByIdQueryHandler(s.Db, new TestCurrentUser(s.Founder.Id))
            .Handle(new GetOrganizationByIdQuery(s.Club.Id), CancellationToken.None);
        get.Errors.Should().ContainSingle(e => e.Code == "Organization.NotFound");
    }

    [Fact]
    public async Task Delete_ByAdmin_ReturnsNotFounder_AndKeepsClub()
    {
        var s = await CreateScenarioAsync();

        var result = await DeleteHandler(s, s.Admin.Id).Handle(
            new DeleteOrganizationCommand(s.Club.Id), CancellationToken.None);

        result.Errors.Should().ContainSingle(e => e.Code == "Organization.NotFounder");
        (await s.Db.Organizations.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task AfterDelete_MembershipOnlyCommands_ReturnNotFound()
    {
        var s = await CreateScenarioAsync();
        (await DeleteHandler(s, s.Founder.Id).Handle(
            new DeleteOrganizationCommand(s.Club.Id), CancellationToken.None)).IsSuccess.Should().BeTrue();

        var rotate = await new RotateInviteCodeCommandHandler(s.Db, new TestCurrentUser(s.Admin.Id), s.Time)
            .Handle(new RotateInviteCodeCommand(s.Club.Id), CancellationToken.None);
        var update = await new UpdateOrganizationCommandHandler(s.Db, new TestCurrentUser(s.Admin.Id), s.Time)
            .Handle(new UpdateOrganizationCommand(s.Club.Id, "Yeni", null, null), CancellationToken.None);

        // Üyelik satırı yüklenemediği için yetki hatası döner; kulüp artık hiçbir komutta çalışmaz.
        rotate.IsSuccess.Should().BeFalse();
        update.IsSuccess.Should().BeFalse();

        var mine = await new ListMyOrganizationsQueryHandler(s.Db, new TestCurrentUser(s.Member.Id))
            .Handle(new ListMyOrganizationsQuery(), CancellationToken.None);
        mine.Value!.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateEvent_ForDeletedClub_ReturnsNotFound()
    {
        var s = await CreateScenarioAsync();
        (await DeleteHandler(s, s.Founder.Id).Handle(
            new DeleteOrganizationCommand(s.Club.Id), CancellationToken.None)).IsSuccess.Should().BeTrue();

        var result = await CreateEventHandler(s, s.Founder.Id).Handle(
            NewCreateEventCommand(s), CancellationToken.None);

        result.Errors.Should().ContainSingle(e => e.Code == "Organization.NotFound");
    }

    // ---- Rotate ----

    [Fact]
    public async Task RotateInviteCode_ByAdmin_Succeeds_ByMember_IsDenied()
    {
        var s = await CreateScenarioAsync();

        var asAdmin = await new RotateInviteCodeCommandHandler(s.Db, new TestCurrentUser(s.Admin.Id), s.Time)
            .Handle(new RotateInviteCodeCommand(s.Club.Id), CancellationToken.None);
        var asMember = await new RotateInviteCodeCommandHandler(s.Db, new TestCurrentUser(s.Member.Id), s.Time)
            .Handle(new RotateInviteCodeCommand(s.Club.Id), CancellationToken.None);

        asAdmin.IsSuccess.Should().BeTrue();
        asAdmin.Value!.CanRotateInviteCode.Should().BeTrue();
        asAdmin.Value.InviteCode.Should().NotBe("ABCD2345");
        asMember.Errors.Should().ContainSingle(e => e.Code == "Organization.CannotManageMembers");
    }

    // ---- Event creation policy ----

    private static CreateEventCommandHandler CreateEventHandler(Scenario s, Guid actorId) =>
        new(s.Db, new TestCurrentUser(actorId), s.Time);

    private static CreateEventCommand NewCreateEventCommand(Scenario s) =>
        new(
            s.Sport.Id,
            "Etkinlik",
            null,
            Now.AddDays(2),
            90,
            41m,
            29m,
            "İstanbul",
            10,
            null,
            null,
            OrganizationId: s.Club.Id);

    private static async Task SetPolicyAsync(Scenario s, short policy)
    {
        var result = await new UpdateOrganizationCommandHandler(s.Db, new TestCurrentUser(s.Founder.Id), s.Time)
            .Handle(
                new UpdateOrganizationCommand(s.Club.Id, "Kulüp", null, null, true, policy),
                CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CreateEvent_ManagersOnly_BlocksMember_AllowsAdmin()
    {
        var s = await CreateScenarioAsync();
        await SetPolicyAsync(s, (short)OrganizationEventCreationPolicy.ManagersOnly);

        var asMember = await CreateEventHandler(s, s.Member.Id).Handle(
            NewCreateEventCommand(s), CancellationToken.None);
        var asAdmin = await CreateEventHandler(s, s.Admin.Id).Handle(
            NewCreateEventCommand(s), CancellationToken.None);

        asMember.Errors.Should().ContainSingle(e => e.Code == "Organization.CannotCreateEvents");
        asAdmin.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CreateEvent_DefaultPolicy_AllowsMember()
    {
        var s = await CreateScenarioAsync();

        var result = await CreateEventHandler(s, s.Member.Id).Handle(
            NewCreateEventCommand(s), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Detail_ReflectsPolicyAndFounderFlags()
    {
        var s = await CreateScenarioAsync();
        await SetPolicyAsync(s, (short)OrganizationEventCreationPolicy.ManagersOnly);

        async Task<Sportner.Application.Features.Organizations.OrganizationDetailResponse> Detail(Guid userId) =>
            (await new GetOrganizationByIdQueryHandler(s.Db, new TestCurrentUser(userId))
                .Handle(new GetOrganizationByIdQuery(s.Club.Id), CancellationToken.None)).Value!;

        var founder = await Detail(s.Founder.Id);
        founder.EventCreationPolicy.Should().Be(1);
        founder.CanCreateEvents.Should().BeTrue();
        founder.CanDelete.Should().BeTrue();
        founder.CanTransferFounder.Should().BeTrue();
        founder.CanRotateInviteCode.Should().BeTrue();

        var admin = await Detail(s.Admin.Id);
        admin.CanCreateEvents.Should().BeTrue();
        admin.CanDelete.Should().BeFalse();
        admin.CanTransferFounder.Should().BeFalse();
        admin.CanRotateInviteCode.Should().BeTrue();

        var member = await Detail(s.Member.Id);
        member.CanCreateEvents.Should().BeFalse();
        member.CanRotateInviteCode.Should().BeFalse();

        var mine = await new ListMyOrganizationsQueryHandler(s.Db, new TestCurrentUser(s.Member.Id))
            .Handle(new ListMyOrganizationsQuery(), CancellationToken.None);
        mine.Value!.Single().CanCreateEvents.Should().BeFalse();
        var mineAdmin = await new ListMyOrganizationsQueryHandler(s.Db, new TestCurrentUser(s.Admin.Id))
            .Handle(new ListMyOrganizationsQuery(), CancellationToken.None);
        mineAdmin.Value!.Single().CanCreateEvents.Should().BeTrue();
    }

    [Theory]
    [InlineData(2)]
    [InlineData(-1)]
    public void UpdateOrganizationValidator_RejectsUnknownPolicy(short policy)
    {
        var validator = new UpdateOrganizationCommandValidator();

        var result = validator.Validate(
            new UpdateOrganizationCommand(Guid.NewGuid(), "Kulüp", null, null, true, policy));

        result.IsValid.Should().BeFalse();
    }
}
