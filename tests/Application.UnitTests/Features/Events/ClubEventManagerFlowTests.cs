using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Features.Events;
using Sportner.Application.Features.Events.CancelEvent;
using Sportner.Application.Features.Events.PendingAttendance;
using Sportner.Application.Features.Events.RemoveAssignedParticipant;
using Sportner.Application.Features.Organizations.ListOrganizationEvents;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Moderation;
using Sportner.Domain.Organizations;
using Sportner.Domain.Sports;
using Sportner.Domain.Users;
using DomainEvent = Sportner.Domain.Events.Event;

namespace Sportner.Application.UnitTests.Features.Events;

/// <summary>Taslak görünürlüğü ve yöneticinin eylemleri (iptal, yoklama, çıkarma).</summary>
public sealed class ClubEventManagerFlowTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    private sealed record Scenario(
        string Db,
        FakeTimeProvider Time,
        User Creator,
        User Admin,
        User Outsider,
        User Player,
        Organization Club,
        DomainEvent Event);

    private enum Phase { Draft, Published, Completed }

    private static async Task<Scenario> SeedAsync(Phase phase)
    {
        var db = Guid.NewGuid().ToString("N");
        var time = new FakeTimeProvider(Now);
        var creator = TestUsers.CreateActive("+905551111111", Now);
        var admin = TestUsers.CreateActive("+905552222222", Now);
        var outsider = TestUsers.CreateActive("+905553333333", Now);
        var player = TestUsers.CreateActive("+905554444444", Now);
        var sport = Sport.Create("Futbol", 1, Now, "futbol");
        var club = Organization.Create(creator.Id, "Kadıköy Spor Kulübü", null, null, "ABCD2345", true, Now);
        var adminMembership = OrganizationMember.CreateApproved(club.Id, admin.Id, Now);
        adminMembership.SetRole(OrganizationRole.Admin, Now);

        var created = Now.AddDays(-2);
        var start = phase == Phase.Completed ? Now.AddHours(-3) : Now.AddDays(2);
        var @event = DomainEvent.Create(
            creator.Id, sport.Id, "Kulüp maçı", start, 60, 41m, 29m, "İstanbul", created,
            maxParticipants: 10, organizationId: club.Id, isOrganizationOnly: false);

        if (phase != Phase.Draft)
        {
            @event.Publish(created);
            @event.Apply(player.Id, created);
            @event.ApproveParticipant(player.Id, created);
        }

        if (phase == Phase.Completed)
        {
            @event.Complete(Now);
        }

        await using var seed = InMemoryDb.Create(db);
        seed.Users.AddRange(creator, admin, outsider, player);
        seed.UserStatistics.Add(UserStatistics.Create(creator.Id, Now));
        seed.Sports.Add(sport);
        seed.Organizations.Add(club);
        seed.OrganizationMembers.AddRange(
            OrganizationMember.CreateFounder(club.Id, creator.Id, Now),
            adminMembership);
        seed.Events.Add(@event);
        await seed.SaveChangesAsync();

        return new Scenario(db, time, creator, admin, outsider, player, club, @event);
    }

    // --- Taslak ----------------------------------------------------------------------------------

    [Fact]
    public async Task Draft_IsHiddenFromOutsiders_InClubListAndDetail_ButVisibleToCreatorAndManager()
    {
        var s = await SeedAsync(Phase.Draft);
        await using var db = InMemoryDb.Create(s.Db);

        async Task<int> ListCount(Guid viewer)
        {
            var handler = new ListOrganizationEventsQueryHandler(db, new TestCurrentUser(viewer), s.Time);
            var result = await handler.Handle(new ListOrganizationEventsQuery(s.Club.Id), CancellationToken.None);
            return result.Value!.Count;
        }

        (await ListCount(s.Outsider.Id)).Should().Be(0);
        (await ListCount(s.Admin.Id)).Should().Be(1);
        (await ListCount(s.Creator.Id)).Should().Be(1);

        (await EventQueries.GetDetailAsync(db, s.Event.Id, s.Outsider.Id, CancellationToken.None)).Should().BeNull();
        (await EventQueries.GetDetailAsync(db, s.Event.Id, null, CancellationToken.None)).Should().BeNull();
        (await EventQueries.GetDetailAsync(db, s.Event.Id, s.Admin.Id, CancellationToken.None)).Should().NotBeNull();
        (await EventQueries.GetDetailAsync(db, s.Event.Id, s.Creator.Id, CancellationToken.None)).Should().NotBeNull();
    }

    [Fact]
    public async Task UpcomingScope_IsCapped_AndValidatorBoundsPage()
    {
        var s = await SeedAsync(Phase.Published);
        await using (var seed = InMemoryDb.Create(s.Db))
        {
            for (var i = 0; i < 60; i++)
            {
                var extra = DomainEvent.Create(
                    s.Creator.Id, s.Event.SportId, $"Etkinlik {i}", Now.AddDays(3 + i), 60, 41m, 29m,
                    "İstanbul", Now, organizationId: s.Club.Id, isOrganizationOnly: false);
                extra.Publish(Now);
                seed.Events.Add(extra);
            }

            await seed.SaveChangesAsync();
        }

        await using var db = InMemoryDb.Create(s.Db);
        var handler = new ListOrganizationEventsQueryHandler(db, new TestCurrentUser(s.Outsider.Id), s.Time);
        var result = await handler.Handle(new ListOrganizationEventsQuery(s.Club.Id), CancellationToken.None);

        result.Value!.Should().HaveCount(50);
        result.Value.Should().BeInAscendingOrder(item => item.EventDate);

        var validator = new ListOrganizationEventsQueryValidator();
        (await validator.ValidateAsync(new ListOrganizationEventsQuery(s.Club.Id, "past", 1001, 20)))
            .IsValid.Should().BeFalse();
        (await validator.ValidateAsync(new ListOrganizationEventsQuery(s.Club.Id, "past", 1000, 20)))
            .IsValid.Should().BeTrue();
    }

    // --- İptal: eylemi yapan ---------------------------------------------------------------------

    [Fact]
    public async Task Cancel_ByManager_NotifiesCreatorAndPlayer_NotManager_AndDoesNotChargeCreator()
    {
        var s = await SeedAsync(Phase.Published);
        await using var db = InMemoryDb.Create(s.Db);
        var publisher = new Mock<INotificationPublisher>();
        var handler = new CancelEventCommandHandler(db, new TestCurrentUser(s.Admin.Id), s.Time, publisher.Object);

        var result = await handler.Handle(new CancelEventCommand(s.Event.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        void Verify(Guid recipient, Times times) =>
            publisher.Verify(p => p.PublishAsync(
                recipient,
                NotificationType.EventCancelled,
                It.Is<string>(title => title.Contains("Kadıköy Spor Kulübü")),
                It.IsAny<string>(),
                NotificationEntityType.Event,
                s.Event.Id,
                s.Admin.Id,
                It.IsAny<CancellationToken>()), times);

        Verify(s.Creator.Id, Times.Once());
        Verify(s.Player.Id, Times.Once());
        Verify(s.Admin.Id, Times.Never());
        (await db.UserStatistics.SingleAsync(x => x.UserId == s.Creator.Id)).EventsCancelled.Should().Be(0);
    }

    [Fact]
    public async Task Cancel_ByCreator_ChargesCreator_AndDoesNotNotifyCreator()
    {
        var s = await SeedAsync(Phase.Published);
        await using var db = InMemoryDb.Create(s.Db);
        var publisher = new Mock<INotificationPublisher>();
        var handler = new CancelEventCommandHandler(db, new TestCurrentUser(s.Creator.Id), s.Time, publisher.Object);

        var result = await handler.Handle(new CancelEventCommand(s.Event.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        publisher.Verify(p => p.PublishAsync(
            s.Creator.Id, NotificationType.EventCancelled, It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<NotificationEntityType>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>()), Times.Never);
        publisher.Verify(p => p.PublishAsync(
            s.Player.Id, NotificationType.EventCancelled, It.IsAny<string>(), It.IsAny<string>(),
            NotificationEntityType.Event, s.Event.Id, s.Creator.Id, It.IsAny<CancellationToken>()), Times.Once);
        (await db.UserStatistics.SingleAsync(x => x.UserId == s.Creator.Id)).EventsCancelled.Should().Be(1);
    }

    // --- Yoklama ---------------------------------------------------------------------------------

    [Fact]
    public async Task PendingAttendance_ListsManagedClubEvents_ForManager_NotOutsider()
    {
        var s = await SeedAsync(Phase.Completed);
        await using var db = InMemoryDb.Create(s.Db);

        async Task<int> CountFor(Guid viewer)
        {
            var handler = new ListPendingAttendanceEventsQueryHandler(db, new TestCurrentUser(viewer));
            return (await handler.Handle(new ListPendingAttendanceEventsQuery(), CancellationToken.None))
                .Value!.Count;
        }

        (await CountFor(s.Creator.Id)).Should().Be(1);
        (await CountFor(s.Admin.Id)).Should().Be(1);
        (await CountFor(s.Outsider.Id)).Should().Be(0);
    }

    [Fact]
    public async Task AttendancePrompt_GoesToCreatorAndClubManagers_Once()
    {
        var s = await SeedAsync(Phase.Completed);
        await using var db = InMemoryDb.Create(s.Db);
        var publisher = new Mock<INotificationPublisher>();
        var @event = await db.Events.SingleAsync(e => e.Id == s.Event.Id);

        await EventCompletion.SendAttendancePromptAsync(db, @event, publisher.Object, CancellationToken.None);

        void Verify(Guid recipient, Times times) =>
            publisher.Verify(p => p.PublishAsync(
                recipient, NotificationType.EventAttendancePending, It.IsAny<string>(), It.IsAny<string>(),
                NotificationEntityType.Event, s.Event.Id, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), times);

        // Kurucu hem oluşturan hem yönetici: tek bildirim.
        Verify(s.Creator.Id, Times.Once());
        Verify(s.Admin.Id, Times.Once());
        Verify(s.Outsider.Id, Times.Never());
        Verify(s.Player.Id, Times.Never());
    }

    // --- Çıkarma kaydı ----------------------------------------------------------------------------

    [Fact]
    public async Task RemoveAssignedParticipant_ByManager_RecordsActingUserInAudit()
    {
        var s = await SeedAsync(Phase.Published);
        var reason = ReportReason.Create("HARASSMENT", "Taciz", null, 1, Now);
        Guid participantId;
        await using (var seed = InMemoryDb.Create(s.Db))
        {
            seed.ReportReasons.Add(reason);
            await seed.SaveChangesAsync();
            participantId = (await seed.EventParticipants.SingleAsync(p => p.UserId == s.Player.Id)).Id;
        }

        await using var db = InMemoryDb.Create(s.Db);
        var handler = new RemoveAssignedParticipantCommandHandler(
            db, new TestCurrentUser(s.Admin.Id), s.Time, Mock.Of<INotificationPublisher>());

        var result = await handler.Handle(
            new RemoveAssignedParticipantCommand(s.Event.Id, participantId, reason.Id, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await db.EventParticipantRemovals.SingleAsync()).OrganizerUserId.Should().Be(s.Admin.Id);
    }
}
