namespace Sportner.Application.DTOs.Reviews;

public record ReviewDto(
    Guid Id,
    Guid EventId,
    Guid ReviewerId,
    string? ReviewerFullName,
    string? ReviewerAvatarUrl,
    Guid ReviewedId,
    int Rating,
    string? Comment,
    DateTime CreatedAt
);

public record CreateReviewDto(
    Guid EventId,
    Guid ReviewedId,
    int Rating,
    string? Comment
);
