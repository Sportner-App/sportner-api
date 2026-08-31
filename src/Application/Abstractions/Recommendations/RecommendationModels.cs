namespace Sportner.Application.Abstractions.Recommendations;

public sealed record Scored<T>(
    T Item,
    double Score,
    IReadOnlyList<string> Reasons);

public sealed record RecommendedPerson(
    Guid UserId,
    string? Username,
    string? FirstName,
    string? ProfileImageUrl,
    string? City,
    int MutualFriendsCount,
    int SharedSportsCount,
    bool SameCity,
    IReadOnlyList<string> SharedSportNames);

public sealed record RecommendedEvent(
    Guid EventId,
    Guid SportId,
    Guid OrganizerUserId,
    DateTimeOffset EventDate,
    decimal Latitude,
    decimal Longitude,
    int? MaxParticipants,
    int ParticipantCount,
    int FriendsAttending,
    bool SportMatch,
    double? DistanceKm);

public sealed record RecommendedPost(
    Guid PostId,
    Guid AuthorUserId,
    DateTimeOffset CreatedAt,
    int LikeCount,
    int CommentCount,
    bool AuthorIsFriend);

public sealed record EventRecommendationRequest(
    Guid? SportId = null,
    string? City = null,
    decimal? Latitude = null,
    decimal? Longitude = null,
    double? RadiusKm = null,
    short? SkillLevel = null,
    int Limit = 20);
