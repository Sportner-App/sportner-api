namespace Sportner.Application.Features.Reviews;

public sealed record ReviewResponse(
    Guid Id,
    Guid EventId,
    Guid ReviewerUserId,
    string? ReviewerUsername,
    string? ReviewerFirstName,
    string? ReviewerProfileImageUrl,
    Guid ReviewedUserId,
    string? ReviewedUsername,
    string? ReviewedFirstName,
    string? ReviewedProfileImageUrl,
    short Rating,
    string? Comment,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record ReviewablePeerResponse(
    Guid UserId,
    string? Username,
    string? FirstName,
    string? ProfileImageUrl);
