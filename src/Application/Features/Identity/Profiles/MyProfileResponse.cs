namespace Sportner.Application.Features.Identity.Profiles;

public sealed record MyProfileResponse(
    Guid UserId,
    string Username,
    string FirstName,
    string? LastName,
    string? Bio,
    short? Gender,
    DateOnly? BirthDate,
    string? City,
    string? ProfileImageUrl,
    string? IntroVideoUrl,
    decimal AverageRating,
    int ReviewCount,
    bool IsProfilePublic,
    DateTimeOffset UsernameChangedAt,
    DateTimeOffset? UsernameChangeAvailableAt,
    IReadOnlyList<ProfileSportResponse> Sports,
    ProfileStatisticsResponse? Statistics);
