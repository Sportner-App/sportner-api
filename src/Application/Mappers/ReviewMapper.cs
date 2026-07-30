using Sportner.Application.DTOs.Reviews;
using Sportner.Domain.Entities;

namespace Sportner.Application.Mappers;

public static class ReviewMapper
{
    public static ReviewDto ToDto(this Review review) => new(
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
