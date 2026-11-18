using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Features.Events;
using Sportner.Application.Features.Events.ApplyToEvent;
using Sportner.Application.Features.Events.ApproveParticipant;
using Sportner.Application.Features.Events.CancelEvent;
using Sportner.Application.Features.Events.EventQuestions.ReplyToEventQuestion;
using Sportner.Application.Features.Events.SetOrganizerParticipation;
using Sportner.Application.Features.Events.UpdateEventDetails;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Events;
using Sportner.Domain.Organizations;
using Sportner.Domain.Sports;
using Sportner.Domain.Users;
using DomainEvent = Sportner.Domain.Events.Event;

namespace Sportner.Application.UnitTests.Features.Events;

/// <summary>
/// Kulüp etkinliğinde kulüp yöneticilerinin ortak yönetimi, kulüp adına bildirim ve
/// organizatörün oyuncu katılımı seçimi.
/// </summary>
public sealed class ClubEventCoManagementTests
{
    private const string ClubName = "Kadıköy Spor Kulübü";

    private static readonly DateTimeOffset Start = new(2026, 11, 18, 10, 0, 0, TimeSpan.Zero);

    private sealed class Scenario
    {
        public required string DatabaseName { get; init; }
        public required FakeTimeProvider Time { get; init; }
        public required User Founder { get; init; }
        public required User Admin { get; init; }
        public required User Member { get; init; }
        public required User OtherClubAdmin { get; init; }
        public required User Applicant { get; init; }
        public required Guid ClubId { get; init; }
        public required Guid EventId { get; init; }
    }

    private static async Task<Scenario> SeedAsync(
        bool organizerParticipates = true,
        int? maxParticipants = 8,
        bool pendingApplicant = false,
        bool deleteClub = false)
    {
        var databaseName = Guid.NewGuid().ToString("N");
        var time = new FakeTimeProvider(Start);
        var now = time.GetUtcNow();

        var founder = TestUsers.CreateActive("+905551111111", now);
        var admin = TestUsers.CreateActive("+905552222222", now);
        var member = TestUsers.CreateActive("+905553333333", now);
        var otherAdmin = TestUsers.CreateActive("+905554444444", now);
        var applicant = TestUsers.CreateActive("+905555555555", now);
        applicant.AttachUserProfile(CreateProfile(applicant.Id, now));
        admin.AttachUserProfile(CreateProfile(admin.Id, now));

        var sport = Sport.Create("Futbol", 1, now, "futbol");

        var club = Organization.Create(founder.Id, ClubName, null, null, "ABCD2345", true, now);
        var otherClub = Organization.Create(otherAdmin.Id, "Başka Kulüp", null, null, "WXYZ2345", true, now);

        var adminMembership = OrganizationMember.CreateApproved(club.Id, admin.Id, now);
        adminMembership.SetRole(OrganizationRole.Admin, now);
        var plainMembership = OrganizationMember.CreateApproved(club.Id, member.Id, now);

        // Başka kulübün yöneticisi: bu etkinlikte hiçbir yetkisi olmamalı.
        var otherAdminMembership = OrganizationMember.CreateFounder(otherClub.Id, otherAdmin.Id, now);

        var @event = DomainEvent.Create(
            founder.Id,
            sport.Id,
            "Kulüp maçı",
            now.AddDays(2),
            90,
            41m,
            29m,
            "İstanbul",
            now,
            maxParticipants: maxParticipants,
            organizationId: club.Id,
            isOrganizationOnly: false,
            organizerParticipates: organizerParticipates);
        @event.Publish(now);

        if (pendingApplicant)
        {
            @event.Apply(applicant.Id, now);
        }

        if (deleteClub)
        {
            club.MarkDeleted(now);
        }

        await using var seed = InMemoryDb.Create(databaseName);
        seed.Users.AddRange(founder, admin, member, otherAdmin, applicant);
        seed.Sports.Add(sport);
        seed.Organizations.AddRange(club, otherClub);
        seed.OrganizationMembers.AddRange(
            OrganizationMember.CreateFounder(club.Id, founder.Id, now),
            adminMembership,
            plainMembership,
            otherAdminMembership);
        seed.Events.Add(@event);
        await seed.SaveChangesAsync();

        return new Scenario
        {
            DatabaseName = databaseName,
            Time = time,
            Founder = founder,
            Admin = admin,
            Member = member,
            OtherClubAdmin = otherAdmin,
            Applicant = applicant,
            ClubId = club.Id,
            EventId = @event.Id,
        };
    }

    private static UserProfile CreateProfile(Guid userId, DateTimeOffset utcNow)
    {
        var profile = UserProfile.Create(userId, $"user-{userId:N}"[..30], "User", utcNow);
        profile.UpdatePersonalDetails(1, new DateOnly(1995, 1, 1), utcNow);
        return profile;
    }

    // --- Ortak yönetim yetkisi -------------------------------------------------------------

    [Fact]
    public async Task UpdateDetails_ClubAdmin_CanEditClubEvent()
    {
        var scenario = await SeedAsync();
        await using var db = InMemoryDb.Create(scenario.DatabaseName);
        var handler = new UpdateEventDetailsCommandHandler(
            db, new TestCurrentUser(scenario.Admin.Id), scenario.Time);

        var result = await handler.Handle(
            new UpdateEventDetailsCommand(scenario.EventId, "Yeni başlık", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Title.Should().Be("Yeni başlık");
    }

    [Fact]
    public async Task UpdateDetails_PlainClubMember_IsRejected()
    {
        var scenario = await SeedAsync();
        await using var db = InMemoryDb.Create(scenario.DatabaseName);
        var handler = new UpdateEventDetailsCommandHandler(
            db, new TestCurrentUser(scenario.Member.Id), scenario.Time);

        var result = await handler.Handle(
            new UpdateEventDetailsCommand(scenario.EventId, "Yeni başlık", null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(error => error.Code == "Event.NotOrganizer");
    }

    [Fact]
    public async Task UpdateDetails_AdminOfAnotherClub_IsRejected()
    {
        var scenario = await SeedAsync();
        await using var db = InMemoryDb.Create(scenario.DatabaseName);
        var handler = new UpdateEventDetailsCommandHandler(
            db, new TestCurrentUser(scenario.OtherClubAdmin.Id), scenario.Time);

        var result = await handler.Handle(
            new UpdateEventDetailsCommand(scenario.EventId, "Yeni başlık", null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(error => error.Code == "Event.NotOrganizer");
    }

    [Fact]
    public async Task UpdateDetails_AdminOfDeletedClub_IsRejected()
    {
        var scenario = await SeedAsync(deleteClub: true);
        await using var db = InMemoryDb.Create(scenario.DatabaseName);
        var handler = new UpdateEventDetailsCommandHandler(
            db, new TestCurrentUser(scenario.Admin.Id), scenario.Time);

        var result = await handler.Handle(
            new UpdateEventDetailsCommand(scenario.EventId, "Yeni başlık", null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ApproveParticipant_ClubAdmin_Approves_AndNotifiesInClubName()
    {
        var scenario = await SeedAsync(pendingApplicant: true);
        await using var db = InMemoryDb.Create(scenario.DatabaseName);
        var publisher = new Mock<INotificationPublisher>();
        var handler = new ApproveParticipantCommandHandler(
            db, new TestCurrentUser(scenario.Admin.Id), scenario.Time, publisher.Object);

        var result = await handler.Handle(
            new ApproveParticipantCommand(scenario.EventId, scenario.Applicant.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await db.EventParticipants.SingleAsync(p => p.UserId == scenario.Applicant.Id))
            .Status.Should().Be(ParticipantStatus.Approved);
        publisher.Verify(p => p.PublishAsync(
            scenario.Applicant.Id,
            NotificationType.EventRequestApproved,
            It.Is<string>(title => title.Contains(ClubName)),
            It.IsAny<string>(),
            NotificationEntityType.Event,
            scenario.EventId,
            It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApproveParticipant_PlainMember_IsRejected()
    {
        var scenario = await SeedAsync(pendingApplicant: true);
        await using var db = InMemoryDb.Create(scenario.DatabaseName);
        var handler = new ApproveParticipantCommandHandler(
            db, new TestCurrentUser(scenario.Member.Id), scenario.Time, Mock.Of<INotificationPublisher>());

        var result = await handler.Handle(
            new ApproveParticipantCommand(scenario.EventId, scenario.Applicant.Id),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        (await db.EventParticipants.SingleAsync(p => p.UserId == scenario.Applicant.Id))
            .Status.Should().Be(ParticipantStatus.Pending);
    }

    [Fact]
    public async Task ApproveParticipant_AdminOfAnotherClub_IsRejected()
    {
        var scenario = await SeedAsync(pendingApplicant: true);
        await using var db = InMemoryDb.Create(scenario.DatabaseName);
        var handler = new ApproveParticipantCommandHandler(
            db, new TestCurrentUser(scenario.OtherClubAdmin.Id), scenario.Time, Mock.Of<INotificationPublisher>());

        var result = await handler.Handle(
            new ApproveParticipantCommand(scenario.EventId, scenario.Applicant.Id),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(error => error.Code == "Event.NotOrganizer");
    }

    // --- Kulüp adına bildirim --------------------------------------------------------------

    [Fact]
    public async Task CancelEvent_ByClubAdmin_NotifiesParticipantsInClubName()
    {
        var scenario = await SeedAsync(pendingApplicant: true);
        await using (var prep = InMemoryDb.Create(scenario.DatabaseName))
        {
            var seeded = await prep.Events.Include(e => e.Participants)
                .SingleAsync(e => e.Id == scenario.EventId);
            seeded.ApproveParticipant(scenario.Applicant.Id, scenario.Time.GetUtcNow());
            await prep.SaveChangesAsync();
        }

        await using var db = InMemoryDb.Create(scenario.DatabaseName);
        var publisher = new Mock<INotificationPublisher>();
        var handler = new CancelEventCommandHandler(
            db, new TestCurrentUser(scenario.Admin.Id), scenario.Time, publisher.Object);

        var result = await handler.Handle(new CancelEventCommand(scenario.EventId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        publisher.Verify(p => p.PublishAsync(
            scenario.Applicant.Id,
            NotificationType.EventCancelled,
            It.Is<string>(title => title.Contains(ClubName)),
            It.IsAny<string>(),
            NotificationEntityType.Event,
            scenario.EventId,
            It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveEventActorName_UsesPersonName_WhenClubIsDeleted()
    {
        var scenario = await SeedAsync(deleteClub: true);
        await using var db = InMemoryDb.Create(scenario.DatabaseName);
        var @event = await db.Events.SingleAsync(e => e.Id == scenario.EventId);

        var actor = await Sportner.Application.Features.Notifications.NotificationActor
            .ResolveEventActorNameAsync(db, @event, scenario.Founder.Id, CancellationToken.None);

        actor.IsClub.Should().BeFalse();
    }

    // --- Başvuru bildirimi yöneticilere ------------------------------------------------------

    [Fact]
    public async Task Apply_ClubEvent_NotifiesCreatorAndClubManagers_Deduplicated_NotPlainMembers()
    {
        var scenario = await SeedAsync();
        await using var db = InMemoryDb.Create(scenario.DatabaseName);
        var publisher = new Mock<INotificationPublisher>();
        var handler = new ApplyToEventCommandHandler(
            db, new TestCurrentUser(scenario.Applicant.Id), scenario.Time, publisher.Object);

        var result = await handler.Handle(new ApplyToEventCommand(scenario.EventId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        void VerifyJoinRequest(Guid recipient, Times times) =>
            publisher.Verify(p => p.PublishAsync(
                recipient,
                NotificationType.EventJoinRequested,
                It.IsAny<string>(),
                It.IsAny<string>(),
                NotificationEntityType.Event,
                scenario.EventId,
                scenario.Applicant.Id,
                It.IsAny<CancellationToken>()), times);

        // Kurucu hem etkinliği oluşturan hem kulüp yöneticisi: tek bildirim.
        VerifyJoinRequest(scenario.Founder.Id, Times.Once());
        VerifyJoinRequest(scenario.Admin.Id, Times.Once());
        VerifyJoinRequest(scenario.Member.Id, Times.Never());
        VerifyJoinRequest(scenario.OtherClubAdmin.Id, Times.Never());
    }

    [Fact]
    public async Task Apply_ByClubAdmin_ExcludesActorFromNotifications()
    {
        var scenario = await SeedAsync();
        // Yönetici de başvurabilir (oluşturan değil); kendine bildirim gitmemeli.
        await using var db = InMemoryDb.Create(scenario.DatabaseName);
        var publisher = new Mock<INotificationPublisher>();
        var handler = new ApplyToEventCommandHandler(
            db, new TestCurrentUser(scenario.Admin.Id), scenario.Time, publisher.Object);

        var result = await handler.Handle(new ApplyToEventCommand(scenario.EventId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        publisher.Verify(p => p.PublishAsync(
            scenario.Admin.Id,
            NotificationType.EventJoinRequested,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<NotificationEntityType>(),
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>()), Times.Never);
        publisher.Verify(p => p.PublishAsync(
            scenario.Founder.Id,
            NotificationType.EventJoinRequested,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<NotificationEntityType>(),
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // --- Detay: CanManage + kulüp alanları ------------------------------------------------------

    [Fact]
    public async Task Detail_CanManage_TrueForCreatorAndClubAdmin_FalseForMemberAndOtherClubAdmin()
    {
        var scenario = await SeedAsync();
        await using var db = InMemoryDb.Create(scenario.DatabaseName);

        async Task<Sportner.Application.Features.Events.EventResponse> Detail(Guid viewer) =>
            (await EventQueries.GetDetailAsync(db, scenario.EventId, viewer, CancellationToken.None))!;

        (await Detail(scenario.Founder.Id)).CanManage.Should().BeTrue();
        var adminDetail = await Detail(scenario.Admin.Id);
        adminDetail.CanManage.Should().BeTrue();
        adminDetail.CanCancel.Should().BeTrue();
        adminDetail.OrganizationName.Should().Be(ClubName);
        adminDetail.IsOrganizerParticipant.Should().BeTrue();
        (await Detail(scenario.Member.Id)).CanManage.Should().BeFalse();
        (await Detail(scenario.OtherClubAdmin.Id)).CanManage.Should().BeFalse();
    }

    // --- Organizatör katılım ucu ------------------------------------------------------------------

    [Fact]
    public async Task SetOrganizerParticipation_Creator_CanLeaveAndRejoin()
    {
        var scenario = await SeedAsync(maxParticipants: 2);
        await using var db = InMemoryDb.Create(scenario.DatabaseName);
        var handler = new SetOrganizerParticipationCommandHandler(
            db, new TestCurrentUser(scenario.Founder.Id), scenario.Time);

        var left = await handler.Handle(
            new SetOrganizerParticipationCommand(scenario.EventId, false), CancellationToken.None);

        left.IsSuccess.Should().BeTrue();
        left.Value!.IsOrganizerParticipant.Should().BeFalse();
        left.Value.OccupiedParticipantCount.Should().Be(0);

        var back = await handler.Handle(
            new SetOrganizerParticipationCommand(scenario.EventId, true), CancellationToken.None);

        back.IsSuccess.Should().BeTrue();
        back.Value!.IsOrganizerParticipant.Should().BeTrue();
        back.Value.OccupiedParticipantCount.Should().Be(1);
    }

    [Fact]
    public async Task SetOrganizerParticipation_OptedOutCreator_CannotJoin_WhenEventIsFull()
    {
        var scenario = await SeedAsync(organizerParticipates: false, maxParticipants: 1, pendingApplicant: true);
        await using (var prep = InMemoryDb.Create(scenario.DatabaseName))
        {
            var seeded = await prep.Events.Include(e => e.Participants)
                .SingleAsync(e => e.Id == scenario.EventId);
            seeded.ApproveParticipant(scenario.Applicant.Id, scenario.Time.GetUtcNow());
            await prep.SaveChangesAsync();
        }

        await using var db = InMemoryDb.Create(scenario.DatabaseName);
        var handler = new SetOrganizerParticipationCommandHandler(
            db, new TestCurrentUser(scenario.Founder.Id), scenario.Time);

        var result = await handler.Handle(
            new SetOrganizerParticipationCommand(scenario.EventId, true), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(error => error.Code == "Event.CapacityFull");
    }

    [Fact]
    public async Task SetOrganizerParticipation_OptedOutCreator_Joins_WhenCapacityAvailable_AddingRow()
    {
        var scenario = await SeedAsync(organizerParticipates: false, maxParticipants: 3);
        await using var db = InMemoryDb.Create(scenario.DatabaseName);
        var handler = new SetOrganizerParticipationCommandHandler(
            db, new TestCurrentUser(scenario.Founder.Id), scenario.Time);

        var result = await handler.Handle(
            new SetOrganizerParticipationCommand(scenario.EventId, true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await db.EventParticipants.SingleAsync(p => p.UserId == scenario.Founder.Id))
            .Status.Should().Be(ParticipantStatus.Approved);
    }

    [Fact]
    public async Task SetOrganizerParticipation_ClubAdmin_IsRejected_BecauseItIsPersonal()
    {
        var scenario = await SeedAsync();
        await using var db = InMemoryDb.Create(scenario.DatabaseName);
        var handler = new SetOrganizerParticipationCommandHandler(
            db, new TestCurrentUser(scenario.Admin.Id), scenario.Time);

        var result = await handler.Handle(
            new SetOrganizerParticipationCommand(scenario.EventId, false), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(error => error.Code == "Event.NotOrganizer");
    }

    [Fact]
    public async Task SetOrganizerParticipation_NonClubEvent_IsRejected()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        var time = new FakeTimeProvider(Start);
        var creator = TestUsers.CreateActive("+905551111111", time.GetUtcNow());
        var sport = Sport.Create("Futbol", 1, time.GetUtcNow(), "futbol");
        var @event = DomainEvent.Create(
            creator.Id, sport.Id, "Normal", time.GetUtcNow().AddDays(1), 90, 41m, 29m, "İstanbul",
            time.GetUtcNow(), maxParticipants: 4);
        @event.Publish(time.GetUtcNow());

        await using (var seed = InMemoryDb.Create(databaseName))
        {
            seed.Users.Add(creator);
            seed.Sports.Add(sport);
            seed.Events.Add(@event);
            await seed.SaveChangesAsync();
        }

        await using var db = InMemoryDb.Create(databaseName);
        var handler = new SetOrganizerParticipationCommandHandler(db, new TestCurrentUser(creator.Id), time);

        var result = await handler.Handle(
            new SetOrganizerParticipationCommand(@event.Id, false), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(error => error.Code == "Event.OrganizerParticipationClubOnly");
    }

    // --- Soru-cevap: kulüp yanıtladı ------------------------------------------------------------

    [Fact]
    public async Task ReplyToQuestion_ByClubAdmin_IsMarkedAnsweredByClub_ButParticipantReplyIsNot()
    {
        var scenario = await SeedAsync();
        var question = EventQuestion.CreateQuestion(
            scenario.EventId, scenario.Member.Id, "Ekipman getirmeli miyiz?", scenario.Time.GetUtcNow());
        await using (var prep = InMemoryDb.Create(scenario.DatabaseName))
        {
            prep.EventQuestions.Add(question);
            await prep.SaveChangesAsync();
        }

        await using var db = InMemoryDb.Create(scenario.DatabaseName);
        var publisher = Mock.Of<INotificationPublisher>();

        var adminReply = await new ReplyToEventQuestionCommandHandler(
                db, new TestCurrentUser(scenario.Admin.Id), scenario.Time, publisher)
            .Handle(
                new ReplyToEventQuestionCommand(scenario.EventId, question.Id, "Getirmeyin, biz sağlıyoruz."),
                CancellationToken.None);

        adminReply.IsSuccess.Should().BeTrue();
        adminReply.Value!.AnsweredByClub.Should().BeTrue();
        adminReply.Value.ClubName.Should().Be(ClubName);
        adminReply.Value.AuthorRole.Should().Be((short)EventQnAAuthorRole.Organizer);

        var memberReply = await new ReplyToEventQuestionCommandHandler(
                db, new TestCurrentUser(scenario.Member.Id), scenario.Time, publisher)
            .Handle(
                new ReplyToEventQuestionCommand(scenario.EventId, question.Id, "Teşekkürler!"),
                CancellationToken.None);

        memberReply.IsSuccess.Should().BeTrue();
        memberReply.Value!.AnsweredByClub.Should().BeFalse();
    }
}
