using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Domain.Reviews;

namespace Sportner.Application.Features.Reviews;

internal static class ReviewRatingSync
{
    /// <summary>
    /// Recomputes the reviewed user's profile and statistics rating/review-count caches from
    /// non-reported reviews, counting only the latest review per reviewer (<see cref="ReviewQueries.LatestPerReviewer"/>).
    /// <para>
    /// <paramref name="excludeReviewId"/> / <paramref name="includeReviewId"/> let callers account for a
    /// review whose IsReported flag was just changed but not saved yet (the query reads the DB):
    /// the excluded review is treated as reported, the included one as not reported.
    /// </para>
    /// </summary>
    internal static async Task SyncReviewedUserAsync(
        IApplicationDbContext dbContext,
        Guid reviewedUserId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken,
        Guid? excludeReviewId = null,
        Guid? includeReviewId = null)
    {
        // Guid.Empty never matches a real review id, so it works as "no override".
        var excludeId = excludeReviewId ?? Guid.Empty;
        var includeId = includeReviewId ?? Guid.Empty;

        var eligibleReviews = ReviewQueries.LatestPerReviewer(
            dbContext.Reviews.AsNoTracking()
                .Where(review => review.ReviewedUserId == reviewedUserId
                    && ((!review.IsReported && review.Id != excludeId) || review.Id == includeId)));

        var aggregates = await eligibleReviews
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Count = group.Count(),
                Average = group.Average(review => (decimal)review.Rating)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var count = aggregates?.Count ?? 0;
        var average = count == 0
            ? 0m
            : decimal.Round(aggregates!.Average, 2, MidpointRounding.AwayFromZero);

        var profile = await dbContext.UserProfiles
            .FirstOrDefaultAsync(candidate => candidate.UserId == reviewedUserId, cancellationToken);

        profile?.UpdateCachedRating(average, count, utcNow);

        var statistics = await dbContext.UserStatistics
            .FirstOrDefaultAsync(candidate => candidate.UserId == reviewedUserId, cancellationToken);

        statistics?.UpdateReviewSummary(average, count, utcNow);
    }
}

internal static class ReviewQueries
{
    /// <summary>
    /// Keeps only the latest review of each (reviewer, reviewed) pair within <paramref name="reviews"/>.
    /// The same person can review the same user once per completed event; without this, repeat
    /// reviews from one reviewer would inflate the reviewed user's rating and review count.
    /// The caller must pass an already filtered set (e.g. non-reported only): the NOT EXISTS
    /// subquery looks at that same set, so a reported latest review does not hide an older valid one.
    /// Tie-break: "latest" is the greatest CreatedAt only (no Id comparison, to keep the SQL
    /// translation trivial); rows with an identical CreatedAt for the same pair are practically
    /// impossible because each belongs to a different event created at a different time.
    /// </summary>
    internal static IQueryable<Review> LatestPerReviewer(IQueryable<Review> reviews) =>
        reviews.Where(review => !reviews.Any(other =>
            other.ReviewerUserId == review.ReviewerUserId
            && other.ReviewedUserId == review.ReviewedUserId
            && other.CreatedAt > review.CreatedAt));

    /// <summary>
    /// Projects reviews (+ reviewer/reviewed profile) into <see cref="ReviewResponse"/>.
    /// Apply all filtering/ordering via <paramref name="configureReviews"/> on the raw
    /// <see cref="Review"/> queryable, not on the returned IQueryable&lt;ReviewResponse&gt; -
    /// EF Core cannot translate a predicate composed against properties of the already
    /// query-constructed record (it fails with "could not be translated") once this
    /// projection's double LEFT JOIN is involved.
    /// </summary>
    internal static IQueryable<ReviewResponse> Project(
        IApplicationDbContext dbContext,
        Func<IQueryable<Review>, IQueryable<Review>>? configureReviews = null,
        bool includeReported = false)
    {
        var reviews = includeReported
            ? dbContext.Reviews.AsNoTracking()
            : dbContext.Reviews.AsNoTracking().Where(review => !review.IsReported);

        if (configureReviews is not null)
        {
            reviews = configureReviews(reviews);
        }

        return
            from review in reviews
            join reviewer in dbContext.UserProfiles.AsNoTracking()
                on review.ReviewerUserId equals reviewer.UserId into reviewers
            from reviewer in reviewers.DefaultIfEmpty()
            join reviewed in dbContext.UserProfiles.AsNoTracking()
                on review.ReviewedUserId equals reviewed.UserId into reviewedProfiles
            from reviewed in reviewedProfiles.DefaultIfEmpty()
            select new ReviewResponse(
                review.Id,
                review.EventId,
                review.ReviewerUserId,
                reviewer != null ? reviewer.Username : null,
                reviewer != null ? reviewer.FirstName : null,
                reviewer != null ? reviewer.ProfileImageUrl : null,
                review.ReviewedUserId,
                reviewed != null ? reviewed.Username : null,
                reviewed != null ? reviewed.FirstName : null,
                reviewed != null ? reviewed.ProfileImageUrl : null,
                review.Rating,
                review.Comment,
                review.CreatedAt,
                review.UpdatedAt);
    }
}
