using System.ComponentModel.DataAnnotations;

namespace SportnerApi.Dtos;

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
    [Required] Guid EventId,
    [Required] Guid ReviewedId,
    [Required, Range(1, 5)] int Rating,
    [MaxLength(1000)] string? Comment
);
