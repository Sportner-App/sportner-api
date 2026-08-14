namespace Sportner.Domain.Common.Constants;

/// <summary>
/// Locked product thresholds for advanced badges (roadmap 07.1 + V2/05).
/// Keep in sync with <c>docs/roadmap/07-product-depth.md</c> and <c>docs/v2/05-badges-depth.md</c>.
/// </summary>
public static class BadgeThresholds
{
    public const int SportsExplorerDistinctSports = 3;

    public const int EventMasterAttended = 10;

    public const int MarathonConsecutiveWeeks = 4;

    public const int CommunityHelperResolvedReports = 5;

    public const int CommunityHelperComments = 20;

    public const int SocialButterflyFriends = 20;

    public const int HostHeroCompletedOrganized = 5;

    public const int ReviewGuruReviewsWritten = 10;

    public const int EarlyBirdMorningAttendances = 5;

    /// <summary>Event local/UTC hour must be strictly before this value.</summary>
    public const int EarlyBirdHourExclusive = 9;
}
