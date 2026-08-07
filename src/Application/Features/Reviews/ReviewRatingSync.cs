using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;

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
    internal static IQueryable<ReviewResponse> Project(
        IApplicationDbContext dbContext,
        bool includeReported = false)
    {
        var reviews = includeReported
            ? dbContext.Reviews.AsNoTracking()
            : dbContext.Reviews.AsNoTracking().Where(review => !review.IsReported);

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
                review.ReviewedUserId,
                reviewed != null ? reviewed.Username : null,
                reviewed != null ? reviewed.FirstName : null,
                review.Rating,
                review.Comment,
                review.CreatedAt,
                review.UpdatedAt);
    }
}
