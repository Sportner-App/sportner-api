using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Storage;
using Sportner.Application.Features.Admin.Announcements;
using Sportner.Application.Features.Admin.Users;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Moderation;
using Sportner.Domain.Notifications;
using Sportner.Domain.Social;
using Sportner.Domain.Users;

namespace Sportner.Application.UnitTests.Features.Admin;

public sealed class AdminAnnouncementAndUserTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 18, 9, 0, 0, TimeSpan.Zero));

    private User NewUser(string email, string? city = null, bool optIn = true, Guid? sportId = null)
    {
        var user = User.RegisterWithExternalProvider(
            ExternalLoginProvider.Google, Guid.NewGuid().ToString(), email, _time.GetUtcNow());
        var profile = UserProfile.Create(user.Id, email.Split('@')[0], "Ad", _time.GetUtcNow());
        if (city is not null)
        {
            profile.UpdateLocation(city, _time.GetUtcNow());
        }

        user.AttachUserProfile(profile);
        if (sportId is { } sport)
        {
            user.AddSport(sport, SkillLevel.Beginner, _time.GetUtcNow());
        }

        return user;
    }

    private NotificationSetting OptIn(User user) =>
        NotificationSetting.Create(user.Id, NotificationType.Announcement, false, true, false, _time.GetUtcNow());

    private static PreviewAnnouncementQueryHandler PreviewHandler(Sportner.Infrastructure.Persistence.AppDbContext db) => new(db);

    private SendAnnouncementCommandHandler SendHandler(
        Sportner.Infrastructure.Persistence.AppDbContext db, Guid adminId, Mock<INotificationPublisher> publisher) =>
        new(db, new TestCurrentUser(adminId), publisher.Object, _time);

    [Fact]
    public async Task Recipients_ExcludeNonConsentingTestAndBannedUsers()
    {
        await using var db = InMemoryDb.Create();
        var ok = NewUser("ok@example.com");
        var noRow = NewUser("norow@example.com");
        var optedOut = NewUser("out@example.com");
        var test = NewUser("test@example.com");
        test.MarkAsTestAccount();
        var banned = NewUser("banned@example.com");
        banned.Ban(_time.GetUtcNow());
        db.Users.AddRange(ok, noRow, optedOut, test, banned);
        db.NotificationSettings.AddRange(
            OptIn(ok), OptIn(test), OptIn(banned),
            NotificationSetting.Create(optedOut.Id, NotificationType.Announcement, false, false, false, _time.GetUtcNow()));
        await db.SaveChangesAsync();

        var result = await PreviewHandler(db).Handle(new PreviewAnnouncementQuery("t", "b", null, null), default);

        result.Value.RecipientCount.Should().Be(1);
    }

    [Fact]
    public async Task Recipients_FilterByCityAndSport()
    {
        await using var db = InMemoryDb.Create();
        var football = Guid.NewGuid();
        var ankaraFootball = NewUser("a@example.com", " ANKARA ", sportId: football);
        var ankaraNone = NewUser("b@example.com", "ankara");
        var istanbulFootball = NewUser("c@example.com", "İstanbul", sportId: football);
        db.Users.AddRange(ankaraFootball, ankaraNone, istanbulFootball);
        db.NotificationSettings.AddRange(OptIn(ankaraFootball), OptIn(ankaraNone), OptIn(istanbulFootball));
        await db.SaveChangesAsync();
        var handler = PreviewHandler(db);

        (await handler.Handle(new PreviewAnnouncementQuery("t", "b", "Ankara", null), default)).Value.RecipientCount.Should().Be(2);
        (await handler.Handle(new PreviewAnnouncementQuery("t", "b", null, football), default)).Value.RecipientCount.Should().Be(2);
        (await handler.Handle(new PreviewAnnouncementQuery("t", "b", "ankara", football), default)).Value.RecipientCount.Should().Be(1);
    }

    [Fact]
    public async Task Send_RejectsStaleExpectedCount_WithoutPublishing()
    {
        await using var db = InMemoryDb.Create();
        var admin = NewUser("boss@example.com");
        var user = NewUser("u@example.com");
        db.Users.AddRange(admin, user);
        db.NotificationSettings.Add(OptIn(user));
        await db.SaveChangesAsync();
        var publisher = new Mock<INotificationPublisher>();

        var result = await SendHandler(db, admin.Id, publisher)
            .Handle(new SendAnnouncementCommand("Başlık", "Mesaj", null, null, 5), default);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be("Admin.AnnouncementAudienceChanged");
        publisher.Verify(p => p.PublishAsync(It.IsAny<Guid>(), It.IsAny<NotificationType>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<NotificationEntityType>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>()), Times.Never);
        (await db.AdminAuditEntries.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Send_PublishesToEveryRecipient_WritesAudit_AndLocksForFiveMinutes()
    {
        await using var db = InMemoryDb.Create();
        var admin = NewUser("boss@example.com");
        var one = NewUser("one@example.com");
        var two = NewUser("two@example.com");
        db.Users.AddRange(admin, one, two);
        db.NotificationSettings.AddRange(OptIn(one), OptIn(two));
        await db.SaveChangesAsync();
        var publisher = new Mock<INotificationPublisher>();
        var handler = SendHandler(db, admin.Id, publisher);

        var first = await handler.Handle(new SendAnnouncementCommand(" Başlık ", "Mesaj", null, null, 2), default);

        first.IsSuccess.Should().BeTrue();
        first.Value.SentCount.Should().Be(2);
        publisher.Verify(p => p.PublishAsync(
            It.IsIn(one.Id, two.Id), NotificationType.Announcement, "Başlık", "Mesaj",
            NotificationEntityType.System, null, null, It.IsAny<CancellationToken>()), Times.Exactly(2));
        var entry = await db.AdminAuditEntries.SingleAsync();
        entry.Action.Should().Be("announcement.send");
        entry.TargetType.Should().Be("announcement");
        entry.Details.Should().Be("Başlık | şehir: tümü | spor: tümü | 2 kişi");

        var second = await handler.Handle(new SendAnnouncementCommand("Başlık", "Mesaj", null, null, 2), default);
        second.IsFailure.Should().BeTrue();
        second.Errors[0].Code.Should().Be("Admin.AnnouncementTooSoon");

        _time.Advance(TimeSpan.FromMinutes(6));
        (await handler.Handle(new SendAnnouncementCommand("Başlık", "Mesaj", null, null, 2), default))
            .IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Send_WithNoRecipients_IsRejected()
    {
        await using var db = InMemoryDb.Create();
        var admin = NewUser("boss@example.com");
        db.Users.Add(admin);
        await db.SaveChangesAsync();

        var result = await SendHandler(db, admin.Id, new Mock<INotificationPublisher>())
            .Handle(new SendAnnouncementCommand("t", "b", null, null, 0), default);

        result.Errors[0].Code.Should().Be("Admin.AnnouncementNoRecipients");
    }

    [Fact]
    public async Task RevokeSessions_CountsActiveOnly_AndWritesAuditOnlyWhenSomethingRevoked()
    {
        await using var db = InMemoryDb.Create();
        var now = _time.GetUtcNow();
        var admin = NewUser("boss@example.com");
        var target = NewUser("t@example.com");
        db.Users.AddRange(admin, target);
        var revoked = UserSession.Create(target.Id, "r", now.AddDays(5), now);
        revoked.Revoke(now);
        db.UserSessions.AddRange(
            UserSession.Create(target.Id, "a", now.AddDays(5), now),
            UserSession.Create(target.Id, "b", now.AddDays(5), now),
            revoked);
        await db.SaveChangesAsync();
        var handler = new RevokeAdminUserSessionsCommandHandler(db, new TestCurrentUser(admin.Id), _time);

        var listed = await new ListAdminUserSessionsQueryHandler(db, _time).Handle(new ListAdminUserSessionsQuery(target.Id), default);
        listed.Value.Should().HaveCount(2);

        var result = await handler.Handle(new RevokeAdminUserSessionsCommand(target.Id), default);
        result.Value.RevokedCount.Should().Be(2);
        (await db.UserSessions.CountAsync(s => s.RevokedAt == null)).Should().Be(0);
        var entry = await db.AdminAuditEntries.SingleAsync();
        entry.Action.Should().Be("user.revoke_sessions");
        entry.Details.Should().Be("2");

        (await handler.Handle(new RevokeAdminUserSessionsCommand(target.Id), default)).Value.RevokedCount.Should().Be(0);
        (await db.AdminAuditEntries.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task UserReports_SeparateAgainstFromBy_AndValidateDirection()
    {
        await using var db = InMemoryDb.Create();
        var now = _time.GetUtcNow();
        var subject = NewUser("subject@example.com");
        var other = NewUser("other@example.com");
        db.Users.AddRange(subject, other);
        var reason = ReportReason.Create("SPAM", "Spam", null, 1, now);
        var ownPost = Post.Create(subject.Id, "x", now);
        var otherPost = Post.Create(other.Id, "y", now);
        db.ReportReasons.Add(reason);
        db.Posts.AddRange(ownPost, otherPost);
        db.Reports.AddRange(
            Report.Create(other.Id, ReportEntityType.User, subject.Id, reason.Id, null, now),
            Report.Create(other.Id, ReportEntityType.Post, ownPost.Id, reason.Id, null, now),
            Report.Create(subject.Id, ReportEntityType.Post, otherPost.Id, reason.Id, null, now),
            Report.Create(subject.Id, ReportEntityType.User, other.Id, reason.Id, null, now));
        await db.SaveChangesAsync();
        var handler = new ListAdminUserReportsQueryHandler(db, Mock.Of<IFileStorage>());

        var against = await handler.Handle(new ListAdminUserReportsQuery(subject.Id, "against", null, null), default);
        var by = await handler.Handle(new ListAdminUserReportsQuery(subject.Id, "by", null, null), default);

        against.Value.Items.Should().HaveCount(2);
        against.Value.Items.Should().OnlyContain(item => item.Reporter!.Id == other.Id);
        by.Value.Items.Should().HaveCount(2);
        by.Value.Items.Should().OnlyContain(item => item.Reporter!.Id == subject.Id);
        (await handler.Handle(new ListAdminUserReportsQuery(subject.Id, "x", null, null), default)).IsFailure.Should().BeTrue();
        (await handler.Handle(new ListAdminUserReportsQuery(Guid.NewGuid(), "by", null, null), default))
            .Errors[0].Code.Should().Be("Admin.UserNotFound");
    }
}
