using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportnerApi.Data;
using SportnerApi.Dtos;
using SportnerApi.Models;

namespace SportnerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController(AppDbContext db) : ControllerBase
{
    private const string ApprovedStatus = "approved";

    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewDto>> CreateReview(
        [FromBody] CreateReviewDto dto,
        CancellationToken cancellationToken = default)
    {
        var reviewerId = GetCurrentUserId();
        if (reviewerId is null)
        {
            return Unauthorized();
        }

        if (dto.ReviewedId == reviewerId.Value)
        {
            return BadRequest(new { message = "Kendinizi değerlendiremezsiniz." });
        }

        var eventEntity = await db.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == dto.EventId, cancellationToken);

        if (eventEntity is null)
        {
            return NotFound(new { message = "Etkinlik bulunamadı." });
        }

        var reviewerParticipated = eventEntity.CreatedBy == reviewerId.Value ||
            await db.EventParticipants.AnyAsync(
                p => p.EventId == dto.EventId &&
                     p.UserId == reviewerId.Value &&
                     p.Status == ApprovedStatus,
                cancellationToken);

        var reviewedParticipated = eventEntity.CreatedBy == dto.ReviewedId ||
            await db.EventParticipants.AnyAsync(
                p => p.EventId == dto.EventId &&
                     p.UserId == dto.ReviewedId &&
                     p.Status == ApprovedStatus,
                cancellationToken);

        if (!reviewerParticipated || !reviewedParticipated)
        {
            return BadRequest(new { message = "Sadece etkinlikte yer alan kullanıcılar birbirini değerlendirebilir." });
        }

        var alreadyReviewed = await db.Reviews.AnyAsync(
            r => r.EventId == dto.EventId &&
                 r.ReviewerId == reviewerId.Value &&
                 r.ReviewedId == dto.ReviewedId,
            cancellationToken);

        if (alreadyReviewed)
        {
            return BadRequest(new { message = "Bu kullanıcıyı bu etkinlik için zaten değerlendirdiniz." });
        }

        var reviewedProfile = await db.Profiles
            .FirstOrDefaultAsync(p => p.Id == dto.ReviewedId, cancellationToken);

        if (reviewedProfile is null)
        {
            return NotFound(new { message = "Değerlendirilecek kullanıcı bulunamadı." });
        }

        var review = new Review
        {
            Id = Guid.NewGuid(),
            EventId = dto.EventId,
            ReviewerId = reviewerId.Value,
            ReviewedId = dto.ReviewedId,
            Rating = dto.Rating,
            Comment = string.IsNullOrWhiteSpace(dto.Comment) ? null : dto.Comment.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        db.Reviews.Add(review);

        var previousCount = reviewedProfile.ReviewCount ?? 0;
        var previousAvg = reviewedProfile.AvgRating ?? 0m;
        var newCount = previousCount + 1;
        reviewedProfile.ReviewCount = newCount;
        reviewedProfile.AvgRating = ((previousAvg * previousCount) + dto.Rating) / newCount;
        reviewedProfile.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        await db.Entry(review).Reference(r => r.Reviewer).LoadAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetReviewsForUser),
            new { userId = dto.ReviewedId },
            MapToDto(review));
    }

    [HttpGet("user/{userId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<ReviewDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ReviewDto>>> GetReviewsForUser(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var reviews = await db.Reviews
            .AsNoTracking()
            .Include(r => r.Reviewer)
            .Where(r => r.ReviewedId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(reviews.Select(MapToDto));
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    private static ReviewDto MapToDto(Review review) => new(
        review.Id,
        review.EventId,
        review.ReviewerId,
        review.Reviewer?.FullName,
        review.Reviewer?.AvatarUrl,
        review.ReviewedId,
        review.Rating,
        review.Comment,
        review.CreatedAt
    );
}
