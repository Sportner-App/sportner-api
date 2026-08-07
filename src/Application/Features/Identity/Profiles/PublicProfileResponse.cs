namespace Sportner.Application.Features.Identity.Profiles;

/// <summary>
/// Public projection: never exposes phone number, birth date or session metadata.
/// </summary>
public sealed record PublicProfileResponse(
    Guid UserId,
    string Username,
    string FirstName,
    string? LastName,
    string? Bio,
    string? City,
    string? ProfileImageUrl,
    string? IntroVideoUrl,
    decimal AverageRating,
    int ReviewCount,
    IReadOnlyList<ProfileSportResponse> Sports,
    ProfileStatisticsResponse? Statistics);
