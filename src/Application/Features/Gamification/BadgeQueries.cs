using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Domain.Badges;
using Sportner.Domain.Common.Constants;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Gamification;

internal static class BadgeQueries
{
    internal static async Task<IReadOnlyList<UserBadgeResponse>> ListForUserAsync(
        IApplicationDbContext dbContext,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await (
                from userBadge in dbContext.UserBadges.AsNoTracking()
                join badge in dbContext.Badges.AsNoTracking()
                    on userBadge.BadgeId equals badge.Id
                where userBadge.UserId == userId && badge.IsActive
                orderby userBadge.IsShowcased descending,
                    userBadge.ShowcaseOrder ascending,
                    userBadge.EarnedAt descending
                select new UserBadgeResponse(
                    userBadge.Id,
                    badge.Id,
                    badge.Code,
                    badge.Name,
                    badge.Description,
                    badge.IconPath,
                    (short)badge.Category,
                    (short)badge.Rarity,
                    badge.ExperiencePoints,
                    userBadge.EarnedAt,
                    userBadge.IsShowcased,
                    userBadge.ShowcaseOrder))
            .ToListAsync(cancellationToken);
    }

    internal static async Task<IReadOnlyList<BadgeProgressItemResponse>> GetProgressAsync(
        IApplicationDbContext dbContext,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var badges = await dbContext.Badges.AsNoTracking()
            .Where(badge => badge.IsActive)
            .OrderBy(badge => badge.DisplayOrder)
            .ThenBy(badge => badge.Code)
            .ToListAsync(cancellationToken);

        var earnedBadgeIds = await dbContext.UserBadges.AsNoTracking()
            .Where(userBadge => userBadge.UserId == userId)
            .Select(userBadge => userBadge.BadgeId)
            .ToListAsync(cancellationToken);
        var earnedSet = earnedBadgeIds.ToHashSet();

        var metrics = await CollectProgressMetricsAsync(dbContext, userId, cancellationToken);
        var items = new List<BadgeProgressItemResponse>(badges.Count);

        foreach (var badge in badges)
        {
            var (current, target) = ResolveProgress(badge.Code, metrics);
            var earned = earnedSet.Contains(badge.Id);
            if (earned)
            {
                current = Math.Max(current, target);
            }

            var percent = target <= 0
                ? (earned ? 100 : 0)
                : Math.Clamp((int)Math.Floor(current * 100.0 / target), 0, 100);

            items.Add(new BadgeProgressItemResponse(
                badge.Id,
                badge.Code,
                badge.Name,
                badge.Description,
                badge.IconPath,
                (short)badge.Category,
                (short)badge.Rarity,
                earned,
                current,
                target,
                percent));
        }

        return items;
    }

    private static async Task<ProgressMetrics> CollectProgressMetricsAsync(
        IApplicationDbContext dbContext,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var attendedCount = await dbContext.EventParticipants.AsNoTracking()
            .CountAsync(
                participant =>
                    participant.UserId == userId
                    && participant.Status == ParticipantStatus.Attended,
                cancellationToken);

        var postsCount = await dbContext.Posts.AsNoTracking()
            .CountAsync(post => post.UserId == userId && !post.IsHidden, cancellationToken);

        var friendsCount = await dbContext.Friendships.AsNoTracking()
            .CountAsync(
                friendship =>
                    friendship.Status == FriendshipStatus.Accepted
                    && (friendship.RequesterUserId == userId
                        || friendship.AddresseeUserId == userId),
                cancellationToken);

        var reviewsWritten = await dbContext.Reviews.AsNoTracking()
            .CountAsync(review => review.ReviewerUserId == userId, cancellationToken);

        var profileSports = await dbContext.UserSports.AsNoTracking()
            .CountAsync(userSport => userSport.UserId == userId, cancellationToken);

        var attendedSports = await (
            from participant in dbContext.EventParticipants.AsNoTracking()
            join @event in dbContext.Events.AsNoTracking() on participant.EventId equals @event.Id
            where participant.UserId == userId
                  && participant.Status == ParticipantStatus.Attended
            select @event.SportId)
            .Distinct()
            .CountAsync(cancellationToken);

        var sportsExplorer = Math.Max(profileSports, attendedSports);

        var resolvedReports = await dbContext.Reports.AsNoTracking()
            .CountAsync(
                report =>
                    report.ReporterUserId == userId
                    && report.Status == ReportStatus.Resolved,
                cancellationToken);

        var comments = await dbContext.PostComments.AsNoTracking()
            .CountAsync(comment => comment.UserId == userId, cancellationToken);

        var communityViaReports = Math.Min(resolvedReports, BadgeThresholds.CommunityHelperResolvedReports);
        var communityViaComments = Math.Min(comments, BadgeThresholds.CommunityHelperComments);
        var preferReports = (communityViaReports * 1.0 / BadgeThresholds.CommunityHelperResolvedReports)
            >= (communityViaComments * 1.0 / BadgeThresholds.CommunityHelperComments);
        var communityCurrent = preferReports ? communityViaReports : communityViaComments;
        var communityTarget = preferReports
            ? BadgeThresholds.CommunityHelperResolvedReports
            : BadgeThresholds.CommunityHelperComments;

        var hostedCompleted = await dbContext.Events.AsNoTracking()
            .CountAsync(
                @event =>
                    @event.OrganizerUserId == userId
                    && @event.Status == EventStatus.Completed,
                cancellationToken);

        var earlyBird = await (
            from participant in dbContext.EventParticipants.AsNoTracking()
            join @event in dbContext.Events.AsNoTracking() on participant.EventId equals @event.Id
            where participant.UserId == userId
                  && participant.Status == ParticipantStatus.Attended
                  && @event.EventDate.Hour < BadgeThresholds.EarlyBirdHourExclusive
            select participant.Id)
            .CountAsync(cancellationToken);

        var eventDates = await (
            from participant in dbContext.EventParticipants.AsNoTracking()
            join @event in dbContext.Events.AsNoTracking() on participant.EventId equals @event.Id
            where participant.UserId == userId
                  && participant.Status == ParticipantStatus.Attended
            select @event.EventDate)
            .ToListAsync(cancellationToken);

        var marathonCurrent = BadgeAwarder.HasConsecutiveWeekStreak(
            eventDates,
            BadgeThresholds.MarathonConsecutiveWeeks)
            ? BadgeThresholds.MarathonConsecutiveWeeks
            : CountBestConsecutiveWeeks(eventDates);

        return new ProgressMetrics(
            Attended: attendedCount,
            Posts: postsCount,
            Friends: friendsCount,
            ReviewsWritten: reviewsWritten,
            SportsExplorer: sportsExplorer,
            CommunityCurrent: communityCurrent,
            CommunityTarget: communityTarget,
            HostedCompleted: hostedCompleted,
            EarlyBird: earlyBird,
            MarathonWeeks: marathonCurrent);
    }

    private static (int Current, int Target) ResolveProgress(string code, ProgressMetrics metrics) =>
        code switch
        {
            BadgeCodes.FirstEvent => (Math.Min(metrics.Attended, 1), 1),
            BadgeCodes.FirstPost => (Math.Min(metrics.Posts, 1), 1),
            BadgeCodes.FirstFriend => (Math.Min(metrics.Friends, 1), 1),
            BadgeCodes.FirstReview => (Math.Min(metrics.ReviewsWritten, 1), 1),
            BadgeCodes.SportsExplorer => (
                Math.Min(metrics.SportsExplorer, BadgeThresholds.SportsExplorerDistinctSports),
                BadgeThresholds.SportsExplorerDistinctSports),
            BadgeCodes.EventMaster => (
                Math.Min(metrics.Attended, BadgeThresholds.EventMasterAttended),
                BadgeThresholds.EventMasterAttended),
            BadgeCodes.CommunityHelper => (metrics.CommunityCurrent, metrics.CommunityTarget),
            BadgeCodes.MarathonRunner => (
                Math.Min(metrics.MarathonWeeks, BadgeThresholds.MarathonConsecutiveWeeks),
                BadgeThresholds.MarathonConsecutiveWeeks),
            BadgeCodes.SocialButterfly => (
                Math.Min(metrics.Friends, BadgeThresholds.SocialButterflyFriends),
                BadgeThresholds.SocialButterflyFriends),
            BadgeCodes.HostHero => (
                Math.Min(metrics.HostedCompleted, BadgeThresholds.HostHeroCompletedOrganized),
                BadgeThresholds.HostHeroCompletedOrganized),
            BadgeCodes.ReviewGuru => (
                Math.Min(metrics.ReviewsWritten, BadgeThresholds.ReviewGuruReviewsWritten),
                BadgeThresholds.ReviewGuruReviewsWritten),
            BadgeCodes.EarlyBird => (
                Math.Min(metrics.EarlyBird, BadgeThresholds.EarlyBirdMorningAttendances),
                BadgeThresholds.EarlyBirdMorningAttendances),
            _ => (0, 1)
        };

    private static int CountBestConsecutiveWeeks(IEnumerable<DateTimeOffset> eventDates)
    {
        var weeks = eventDates
            .Select(value =>
            {
                var date = DateOnly.FromDateTime(value.UtcDateTime);
                var dt = date.ToDateTime(TimeOnly.MinValue);
                return (Year: System.Globalization.ISOWeek.GetYear(dt),
                    Week: System.Globalization.ISOWeek.GetWeekOfYear(dt));
            })
            .Distinct()
            .OrderBy(week => week.Year)
            .ThenBy(week => week.Week)
            .ToList();

        if (weeks.Count == 0)
        {
            return 0;
        }

        var best = 1;
        var streak = 1;
        for (var i = 1; i < weeks.Count; i++)
        {
            var previous = weeks[i - 1];
            var current = weeks[i];
            var consecutive = previous.Year == current.Year
                ? current.Week == previous.Week + 1
                : current.Year == previous.Year + 1
                    && current.Week == 1
                    && previous.Week == System.Globalization.ISOWeek.GetWeekOfYear(
                        new DateTime(previous.Year, 12, 28));

            if (consecutive)
            {
                streak++;
                best = Math.Max(best, streak);
            }
            else
            {
                streak = 1;
            }
        }

        return best;
    }

    private sealed record ProgressMetrics(
        int Attended,
        int Posts,
        int Friends,
        int ReviewsWritten,
        int SportsExplorer,
        int CommunityCurrent,
        int CommunityTarget,
        int HostedCompleted,
        int EarlyBird,
        int MarathonWeeks);
}
