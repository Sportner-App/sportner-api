using System.Text.Json;

namespace Sportner.Application.DTOs.Users;

public record UserDto(
    Guid UserId,
    string Email,
    string? FullName,
    string? AvatarUrl,
    string? Bio,
    List<string>? Sports,
    string? IntroVideoUrl,
    bool IsOnboarded,
    DateTime? BirthDate,
    JsonElement? SkillLevels,
    decimal? AvgRating,
    int? ReviewCount,
    string? PushToken,
    DateTime? UpdatedAt
);

public record UpdateUserDto(
    string? FullName,
    string? AvatarUrl,
    string? Bio,
    List<string>? Sports,
    string? IntroVideoUrl,
    DateTime? BirthDate,
    bool? IsOnboarded,
    JsonElement? SkillLevels
);

public record AvatarUploadResponseDto(
    string AvatarUrl
);
