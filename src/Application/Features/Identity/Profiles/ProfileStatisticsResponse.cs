namespace Sportner.Application.Features.Identity.Profiles;

/// <summary>
/// Read-only counters owned by other modules. Clients can never write these.
/// </summary>
public sealed record ProfileStatisticsResponse(
    int EventsJoined,
    int EventsOrganized,
    int EventsCompleted,
    int EventsCancelled,
    decimal AttendanceRate,
    decimal AverageRating,
    int TotalReviews,
    int FriendsCount,
    int PostsCount,
    int BadgesCount);
