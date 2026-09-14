using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Domain.Reviews;

namespace Sportner.Application.Features.Reviews;

internal static class ReviewRatingSync
{
    /// <summary>
    /// Recomputes the reviewed user's profile and statistics rating caches from non-reported reviews.
    /// </summary>
    internal static async Task SyncReviewedUserAsync(
        IApplicationDbContext dbContext,
        Guid reviewedUserId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var aggregates = await dbContext.Reviews.AsNoTracking()
            .Where(review => review.ReviewedUserId == reviewedUserId && !review.IsReported)
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

        statistics?.UpdateAverageRating(average, utcNow);
    }
}

internal static class ReviewQueries
{
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
