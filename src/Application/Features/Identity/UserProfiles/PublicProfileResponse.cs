namespace Sportner.Application.Features.Identity.UserProfiles;

/// <summary>
/// Public projection: never exposes phone number, raw birth date or session metadata —
/// only the derived <see cref="Age"/> in years.
/// </summary>
public sealed record PublicProfileResponse(
    Guid UserId,
    string Username,
    string FirstName,
    string? LastName,
    string? Bio,
    short? Gender,
    int? Age,
    string? City,
    string? ProfileImageUrl,
    decimal AverageRating,
    int ReviewCount,
    IReadOnlyList<ProfileSportResponse> Sports,
    ProfileStatisticsResponse? Statistics,
    ProfileFriendshipResponse? Friendship = null);

/// <summary>
/// Viewer-relative friendship with the profile owner. Null when anonymous, self, or no row.
/// </summary>
public sealed record ProfileFriendshipResponse(
    Guid FriendshipId,
    short Status,
    Guid RequesterUserId,
    Guid AddresseeUserId);
