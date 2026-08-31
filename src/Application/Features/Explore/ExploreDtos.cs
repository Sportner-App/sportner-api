namespace Sportner.Application.Features.Explore;

/// <summary>People tab item — same shape as friend suggestions; score stays server-side.</summary>
public sealed record ExplorePersonItemResponse(
    Guid UserId,
    string? Username,
    string? FirstName,
    string? ProfileImageUrl,
    string? City,
    int MutualFriendsCount,
    int SharedSportsCount,
    bool SameCity,
    IReadOnlyList<string> SharedSportNames);

public sealed record ExploreEventItemResponse(
    Guid Id,
    Guid SportId,
    string SportName,
    string SportSlug,
    string? SportCoverImageUrl,
    Guid OrganizerUserId,
    string? OrganizerUsername,
    string Title,
    DateTimeOffset EventDate,
    int DurationMinutes,
    string Address,
    int? MaxParticipants,
    short? SkillLevel,
    short Status,
    int OccupiedParticipantCount,
    double? DistanceKm,
    int FriendsAttending,
    bool SportMatch);
