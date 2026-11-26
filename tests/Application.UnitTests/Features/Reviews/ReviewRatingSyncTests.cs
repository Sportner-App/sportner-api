using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Features.Moderation;
using Sportner.Application.Features.Reviews;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Moderation;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Reviews;
using Sportner.Domain.Users;

namespace Sportner.Application.UnitTests.Features.Reviews;

public sealed class ReviewRatingSyncTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Sync_SameReviewerTwoEvents_CountsOnlyLatestReview()
    {
        await using var db = InMemoryDb.Create();
        var reviewedId = await SeedReviewedAsync(db);
        var reviewerId = Guid.NewGuid();

        db.Reviews.Add(Review.Create(Guid.NewGuid(), reviewerId, reviewedId, 1, null, Now.AddDays(-2)));
        db.Reviews.Add(Review.Create(Guid.NewGuid(), reviewerId, reviewedId, 5, null, Now.AddDays(-1)));
        await db.SaveChangesAsync();

        await ReviewRatingSync.SyncReviewedUserAsync(db, reviewedId, Now, CancellationToken.None);
        await db.SaveChangesAsync();

        var profile = await db.UserProfiles.SingleAsync(p => p.UserId == reviewedId);
        var statistics = await db.UserStatistics.SingleAsync(s => s.UserId == reviewedId);
        profile.ReviewCount.Should().Be(1);
        profile.AverageRating.Should().Be(5m);
        statistics.TotalReviews.Should().Be(1);
        statistics.AverageRating.Should().Be(5m);
    }

    [Fact]
    public async Task Sync_LatestReviewReported_FallsBackToOlderReview()
    {
        await using var db = InMemoryDb.Create();
        var reviewedId = await SeedReviewedAsync(db);
        var reviewerId = Guid.NewGuid();

        var older = Review.Create(Guid.NewGuid(), reviewerId, reviewedId, 2, null, Now.AddDays(-2));
        var latest = Review.Create(Guid.NewGuid(), reviewerId, reviewedId, 5, null, Now.AddDays(-1));
        latest.MarkAsReported(Now);
        db.Reviews.AddRange(older, latest);
        await db.SaveChangesAsync();

        await ReviewRatingSync.SyncReviewedUserAsync(db, reviewedId, Now, CancellationToken.None);
        await db.SaveChangesAsync();

        var profile = await db.UserProfiles.SingleAsync(p => p.UserId == reviewedId);
        var statistics = await db.UserStatistics.SingleAsync(s => s.UserId == reviewedId);
        profile.ReviewCount.Should().Be(1);
        profile.AverageRating.Should().Be(2m);
        statistics.TotalReviews.Should().Be(1);
    }

    [Fact]
    public async Task Sync_DifferentReviewers_CountsBoth()
    {
        await using var db = InMemoryDb.Create();
        var reviewedId = await SeedReviewedAsync(db);

        db.Reviews.Add(Review.Create(Guid.NewGuid(), Guid.NewGuid(), reviewedId, 4, null, Now.AddDays(-2)));
        db.Reviews.Add(Review.Create(Guid.NewGuid(), Guid.NewGuid(), reviewedId, 2, null, Now.AddDays(-1)));
        await db.SaveChangesAsync();

        await ReviewRatingSync.SyncReviewedUserAsync(db, reviewedId, Now, CancellationToken.None);
        await db.SaveChangesAsync();

        var profile = await db.UserProfiles.SingleAsync(p => p.UserId == reviewedId);
        var statistics = await db.UserStatistics.SingleAsync(s => s.UserId == reviewedId);
        profile.ReviewCount.Should().Be(2);
        profile.AverageRating.Should().Be(3m);
        statistics.TotalReviews.Should().Be(2);
    }

    [Fact]
    public async Task Sync_AllReviewsReported_ResetsToZero()
    {
        await using var db = InMemoryDb.Create();
        var reviewedId = await SeedReviewedAsync(db);

        var review = Review.Create(Guid.NewGuid(), Guid.NewGuid(), reviewedId, 4, null, Now.AddDays(-1));
        review.MarkAsReported(Now);
        db.Reviews.Add(review);
        await db.SaveChangesAsync();

        await ReviewRatingSync.SyncReviewedUserAsync(db, reviewedId, Now, CancellationToken.None);
        await db.SaveChangesAsync();

        var profile = await db.UserProfiles.SingleAsync(p => p.UserId == reviewedId);
        var statistics = await db.UserStatistics.SingleAsync(s => s.UserId == reviewedId);
        profile.AverageRating.Should().Be(0m);
        profile.ReviewCount.Should().Be(0);
        statistics.TotalReviews.Should().Be(0);
    }

    [Fact]
    public async Task ApplyReviewSideEffects_MarkLatestReported_WithoutIntermediateSave_FallsBackToOlder()
    {
        await using var db = InMemoryDb.Create();
        var reviewedId = await SeedReviewedAsync(db);
        var reviewerId = Guid.NewGuid();

        var older = Review.Create(Guid.NewGuid(), reviewerId, reviewedId, 2, null, Now.AddDays(-2));
        var latest = Review.Create(Guid.NewGuid(), reviewerId, reviewedId, 5, null, Now.AddDays(-1));
        db.Reviews.AddRange(older, latest);
        await db.SaveChangesAsync();
        await ReviewRatingSync.SyncReviewedUserAsync(db, reviewedId, Now, CancellationToken.None);
        await db.SaveChangesAsync();

        var report = Report.Create(Guid.NewGuid(), ReportEntityType.Review, latest.Id, Guid.NewGuid(), null, Now);
        await ReportQueries.ApplyReviewSideEffectsAsync(db, report, markReported: true, Now, CancellationToken.None);
        await db.SaveChangesAsync();

        var profile = await db.UserProfiles.SingleAsync(p => p.UserId == reviewedId);
        var statistics = await db.UserStatistics.SingleAsync(s => s.UserId == reviewedId);
        profile.ReviewCount.Should().Be(1);
        profile.AverageRating.Should().Be(2m);
        statistics.TotalReviews.Should().Be(1);
    }

    [Fact]
    public async Task ApplyReviewSideEffects_ClearReported_CountsReviewAgain()
    {
        await using var db = InMemoryDb.Create();
        var reviewedId = await SeedReviewedAsync(db);
        var reviewerId = Guid.NewGuid();

        var older = Review.Create(Guid.NewGuid(), reviewerId, reviewedId, 2, null, Now.AddDays(-2));
        var latest = Review.Create(Guid.NewGuid(), reviewerId, reviewedId, 5, null, Now.AddDays(-1));
        latest.MarkAsReported(Now);
        db.Reviews.AddRange(older, latest);
        await db.SaveChangesAsync();
        await ReviewRatingSync.SyncReviewedUserAsync(db, reviewedId, Now, CancellationToken.None);
        await db.SaveChangesAsync();

        var report = Report.Create(Guid.NewGuid(), ReportEntityType.Review, latest.Id, Guid.NewGuid(), null, Now);
        await ReportQueries.ApplyReviewSideEffectsAsync(db, report, markReported: false, Now, CancellationToken.None);
        await db.SaveChangesAsync();

        var profile = await db.UserProfiles.SingleAsync(p => p.UserId == reviewedId);
        var statistics = await db.UserStatistics.SingleAsync(s => s.UserId == reviewedId);
        profile.ReviewCount.Should().Be(1);
        profile.AverageRating.Should().Be(5m);
        statistics.TotalReviews.Should().Be(1);
    }

    private static async Task<Guid> SeedReviewedAsync(Sportner.Infrastructure.Persistence.AppDbContext db)
    {
        var reviewedId = Guid.NewGuid();
        db.UserProfiles.Add(UserProfile.Create(reviewedId, "reviewed", "Reviewed", Now));
        db.UserStatistics.Add(UserStatistics.Create(reviewedId, Now));
        await db.SaveChangesAsync();
        return reviewedId;
    }
}
