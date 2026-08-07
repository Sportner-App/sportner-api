using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Sportner.Application.Features.Moderation.CreateReport;
using Sportner.Application.Features.Moderation.RejectReport;
using Sportner.Application.Features.Moderation.ResolveReport;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Moderation;
using Sportner.Domain.Reviews;
using Sportner.Domain.Sports;
using Sportner.Domain.Users;
using Sportner.Infrastructure.Persistence;
using DomainEvent = Sportner.Domain.Events.Event;

namespace Sportner.Application.UnitTests.Features.Moderation;

public sealed class ReportHandlerTests
{
    [Fact]
    public async Task CreateReport_Fails_OnDuplicate()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 8, 0, 0, TimeSpan.Zero));
        var (reporter, _, review, reason) = await SeedReviewReportTargetAsync(db, time);

        var handler = new CreateReportCommandHandler(
            db,
            new TestCurrentUser(reporter.Id),
            time);

        var first = await handler.Handle(
            new CreateReportCommand(
                (short)ReportEntityType.Review,
                review.Id,
                reason.Id,
                "spam"),
            CancellationToken.None);

        first.IsSuccess.Should().BeTrue(because: string.Join("; ", first.Errors.Select(e => e.Code)));

        var second = await handler.Handle(
            new CreateReportCommand(
                (short)ReportEntityType.Review,
                review.Id,
                reason.Id,
                "again"),
            CancellationToken.None);

        second.IsSuccess.Should().BeFalse();
        second.Errors.Should().Contain(error => error.Code == "Report.AlreadyExists");
    }

    [Fact]
    public async Task ResolveKeepsReportedFlag_RejectClearsIt()
    {
        await using var db = InMemoryDb.Create();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 8, 0, 0, TimeSpan.Zero));
        var (reporter, moderator, review, reason) = await SeedReviewReportTargetAsync(db, time);

        var createHandler = new CreateReportCommandHandler(
            db,
            new TestCurrentUser(reporter.Id),
            time);

        var created = await createHandler.Handle(
            new CreateReportCommand(
                (short)ReportEntityType.Review,
                review.Id,
                reason.Id,
                "abuse"),
            CancellationToken.None);

        created.IsSuccess.Should().BeTrue(because: string.Join("; ", created.Errors.Select(e => e.Code)));
        review.IsReported.Should().BeTrue();

        var resolveHandler = new ResolveReportCommandHandler(
            db,
            new TestCurrentUser(moderator.Id),
            time);

        var resolved = await resolveHandler.Handle(
            new ResolveReportCommand(created.Value!.Id, "Action taken"),
            CancellationToken.None);

        resolved.IsSuccess.Should().BeTrue();
        review.IsReported.Should().BeTrue();

        var otherReview = Review.Create(
            review.EventId,
            moderator.Id,
            review.ReviewedUserId,
            rating: 2,
            comment: "meh",
            time.GetUtcNow());
        db.Reviews.Add(otherReview);
        await db.SaveChangesAsync();

        var secondReport = await createHandler.Handle(
            new CreateReportCommand(
                (short)ReportEntityType.Review,
                otherReview.Id,
                reason.Id,
                "false"),
            CancellationToken.None);

        secondReport.IsSuccess.Should().BeTrue();
        otherReview.IsReported.Should().BeTrue();

        var rejectHandler = new RejectReportCommandHandler(
            db,
            new TestCurrentUser(moderator.Id),
            time);

        var rejected = await rejectHandler.Handle(
            new RejectReportCommand(secondReport.Value!.Id, "No violation"),
            CancellationToken.None);

        rejected.IsSuccess.Should().BeTrue();
        otherReview.IsReported.Should().BeFalse();
    }

    private static async Task<(User Reporter, User Moderator, Review Review, ReportReason Reason)>
        SeedReviewReportTargetAsync(AppDbContext db, FakeTimeProvider time)
    {
        var reporter = TestUsers.CreateActive("+905551111111", time.GetUtcNow());
        var reviewer = TestUsers.CreateActive("+905552222222", time.GetUtcNow());
        var reviewed = TestUsers.CreateActive("+905553333333", time.GetUtcNow());
        var moderator = TestUsers.CreateActive("+905554444444", time.GetUtcNow());
        var sport = Sport.Create("Yoga", 1, time.GetUtcNow(), "yoga");

        var @event = DomainEvent.Create(
            reviewed.Id,
            sport.Id,
            "Session",
            time.GetUtcNow().AddHours(1),
            durationMinutes: 45,
            latitude: 41m,
            longitude: 29m,
            address: "Istanbul",
            time.GetUtcNow());

        // Reviewer authors the review; a third party reports it (cannot report own review).
        var review = Review.Create(
            @event.Id,
            reviewer.Id,
            reviewed.Id,
            rating: 4,
            comment: "nice",
            time.GetUtcNow());

        var reason = ReportReason.Create("SPAM", "Spam", null, 1, time.GetUtcNow());

        db.Users.AddRange(reporter, reviewer, reviewed, moderator);
        db.Sports.Add(sport);
        db.Events.Add(@event);
        db.Reviews.Add(review);
        db.ReportReasons.Add(reason);
        await db.SaveChangesAsync();

        return (reporter, moderator, review, reason);
    }
}
