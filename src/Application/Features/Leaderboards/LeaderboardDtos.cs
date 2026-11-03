namespace Sportner.Application.Features.Leaderboards;

public enum LeaderboardPeriod
{
    Month = 0,
    AllTime = 1
}

public sealed record LeaderboardEntryResponse(
    int Rank,
    Guid UserId,
    string Username,
    string FullName,
    string? ProfileImageUrl,
    int EventCount);

public sealed record DistrictLeaderboardResponse(
    string City,
    string District,
    LeaderboardPeriod Period,
    int TotalPlayers,
    int MinPlayers,
    IReadOnlyList<LeaderboardEntryResponse> Entries,
    LeaderboardEntryResponse? Me);

public sealed record DistrictSummaryResponse(string City, string District, int PlayersThisMonth, int EventsAllTime);

public sealed record MyDistrictStandingResponse(
    string City,
    string District,
    int? Rank,
    int EventCount,
    int TotalPlayers);

public sealed record DistrictCrownResponse(string City, string District, DateOnly Month, int EventCount);
