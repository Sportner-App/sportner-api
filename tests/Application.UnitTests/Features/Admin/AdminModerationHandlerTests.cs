using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Sportner.Application.Abstractions.Gamification;
using Sportner.Application.Abstractions.Storage;
using Sportner.Application.Features.Admin.Reports;
using Sportner.Application.Features.Admin.Users;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Moderation;
using Sportner.Domain.Social;
using Sportner.Domain.Users;

namespace Sportner.Application.UnitTests.Features.Admin;

public sealed class AdminModerationHandlerTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 18, 9, 0, 0, TimeSpan.Zero));

    private User NewUser(string email) =>
        User.RegisterWithExternalProvider(ExternalLoginProvider.Google, Guid.NewGuid().ToString(), email, _time.GetUtcNow());

    private Report NewReport(Guid reporterId, ReportEntityType type, Guid entityId, Guid reasonId) =>
        Report.Create(reporterId, type, entityId, reasonId, "açıklama", _time.GetUtcNow());

    [Fact]
    public async Task Resolve_HidesPost_AndWritesAuditEntry()
    {
        await using var db = InMemoryDb.Create();
        var admin = NewUser("boss@example.com");
        var author = NewUser("author@example.com");
        var reporter = NewUser("reporter@example.com");
        var reason = ReportReason.Create("SPAM", "Spam", null, 1, _time.GetUtcNow());
        var post = Post.Create(author.Id, "uygunsuz", _time.GetUtcNow());
        var report = NewReport(reporter.Id, ReportEntityType.Post, post.Id, reason.Id);
        db.Users.AddRange(admin, author, reporter);
        db.ReportReasons.Add(reason);
        db.Posts.Add(post);
        db.Reports.Add(report);
        await db.SaveChangesAsync();

        var badges = new Mock<IBadgeAwarder>();
        var handler = new AdminResolveReportCommandHandler(db, new TestCurrentUser(admin.Id), _time, badges.Object);

        (await handler.Handle(new AdminResolveReportCommand(report.Id, "  kural ihlali "), default))
            .IsSuccess.Should().BeTrue();

        (await db.Posts.SingleAsync()).IsHidden.Should().BeTrue();
        var saved = await db.Reports.SingleAsync();
        saved.Status.Should().Be(ReportStatus.Resolved);
        saved.ReviewedByUserId.Should().Be(admin.Id);
        var entry = await db.AdminAuditEntries.SingleAsync();
        entry.Action.Should().Be("report.resolve");
        entry.TargetType.Should().Be("report");
        entry.TargetId.Should().Be(report.Id);
        entry.Details.Should().Be("kural ihlali");
        badges.Verify(b => b.EvaluateAfterReportResolvedAsync(reporter.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reject_DoesNotUnhideHiddenPost()
    {
        await using var db = InMemoryDb.Create();
        var admin = NewUser("boss@example.com");
        var author = NewUser("author@example.com");
        var reporter = NewUser("reporter@example.com");
        var reason = ReportReason.Create("SPAM", "Spam", null, 1, _time.GetUtcNow());
        var post = Post.Create(author.Id, "gizli", _time.GetUtcNow());
        post.Hide(_time.GetUtcNow());
        var report = NewReport(reporter.Id, ReportEntityType.Post, post.Id, reason.Id);
        db.Users.AddRange(admin, author, reporter);
        db.ReportReasons.Add(reason);
        db.Posts.Add(post);
        db.Reports.Add(report);
        await db.SaveChangesAsync();

        var handler = new AdminRejectReportCommandHandler(db, new TestCurrentUser(admin.Id), _time);

        (await handler.Handle(new AdminRejectReportCommand(report.Id, "geçersiz"), default))
            .IsSuccess.Should().BeTrue();

        (await db.Posts.SingleAsync()).IsHidden.Should().BeTrue("admin reject hedefe dokunmaz");
        (await db.Reports.SingleAsync()).Status.Should().Be(ReportStatus.Rejected);
        (await db.AdminAuditEntries.SingleAsync()).Action.Should().Be("report.reject");
    }

    [Fact]
    public async Task List_IncludesTestAccountContent_AndCountsOpenReportsOnTarget()
    {
        await using var db = InMemoryDb.Create();
        var author = NewUser("author@example.com");
        author.MarkAsTestAccount();
        var reporterA = NewUser("a@example.com");
        var reporterB = NewUser("b@example.com");
        var reporterC = NewUser("c@example.com");
        var reason = ReportReason.Create("SPAM", "Spam", null, 1, _time.GetUtcNow());
        var post = Post.Create(author.Id, "test hesabının gönderisi", _time.GetUtcNow());
        post.Hide(_time.GetUtcNow());
        var open1 = NewReport(reporterA.Id, ReportEntityType.Post, post.Id, reason.Id);
        var open2 = NewReport(reporterB.Id, ReportEntityType.Post, post.Id, reason.Id);
        var closed = NewReport(reporterC.Id, ReportEntityType.Post, post.Id, reason.Id);
        closed.Reject(reporterA.Id, "geçersiz", _time.GetUtcNow());
        var missing = NewReport(reporterA.Id, ReportEntityType.Comment, Guid.NewGuid(), reason.Id);
        db.Users.AddRange(author, reporterA, reporterB, reporterC);
        db.ReportReasons.Add(reason);
        db.Posts.Add(post);
        db.Reports.AddRange(open1, open2, closed, missing);
        await db.SaveChangesAsync();

        var handler = new ListAdminReportsQueryHandler(db, Mock.Of<IFileStorage>());
        var result = await handler.Handle(new ListAdminReportsQuery(null, (short)ReportEntityType.Post, 1, 25), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(3);
        result.Value.Items.Should().OnlyContain(item => item.OpenReportsOnTarget == 2);
        result.Value.Items.Should().OnlyContain(item => item.Target.Exists && item.Target.State == "hidden");
        result.Value.Items.Should().OnlyContain(item => item.Target.Owner!.Id == author.Id);
        result.Value.Items.Should().OnlyContain(item => item.ReasonCode != null && item.Reporter != null);

        var all = await handler.Handle(new ListAdminReportsQuery(null, null, 1, 25), default);
        var gone = all.Value!.Items.Single(item => item.EntityType == (short)ReportEntityType.Comment);
        gone.Target.Exists.Should().BeFalse();
        gone.Target.State.Should().Be("missing");
    }

    [Fact]
    public async Task SetTestAccount_TogglesFlag_AndWritesAuditEntry()
    {
        await using var db = InMemoryDb.Create();
        var admin = NewUser("boss@example.com");
        var target = NewUser("target@example.com");
        db.Users.AddRange(admin, target);
        await db.SaveChangesAsync();

        var handler = new SetTestAccountCommandHandler(db, new TestCurrentUser(admin.Id), _time);

        (await handler.Handle(new SetTestAccountCommand(target.Id, true), default)).IsSuccess.Should().BeTrue();
        (await db.Users.SingleAsync(user => user.Id == target.Id)).IsTestAccount.Should().BeTrue();

        (await handler.Handle(new SetTestAccountCommand(target.Id, false), default)).IsSuccess.Should().BeTrue();
        (await db.Users.SingleAsync(user => user.Id == target.Id)).IsTestAccount.Should().BeFalse();

        var entries = await db.AdminAuditEntries.OrderBy(entry => entry.CreatedAt).ToListAsync();
        entries.Should().HaveCount(2);
        entries.Select(entry => entry.Details).Should().BeEquivalentTo(new[] { "on", "off" });
        entries.Should().OnlyContain(entry => entry.Action == "user.test_account" && entry.TargetId == target.Id);

        (await handler.Handle(new SetTestAccountCommand(Guid.NewGuid(), true), default))
            .IsSuccess.Should().BeFalse();
    }
}
