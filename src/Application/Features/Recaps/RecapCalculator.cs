using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Localization;
using Sportner.Application.Features.Identity.UserProfiles.Streaks;
using Sportner.Application.Features.Leaderboards;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Recaps;

/// <summary>
/// Aylık ve yıllık özet: karne, seri, challenge ve sıralamayla aynı kural (yoklamada "geldi"
/// işaretlenen ya da organizatörü olunan tamamlanmış etkinlikler, etkinlik başına
/// bir kez). Ay ve yıl İstanbul takvimine göre.
/// </summary>
internal static class RecapCalculator
{
    /// <summary>Bundan az maçla özet gösterilmez; "en çok Ahmet'le oynadın" tek maçta anlamsız.</summary>
    internal const int MinEvents = 2;

    internal sealed record Played(
        Guid EventId,
        Guid SportId,
        int Minutes,
        string? District,
        DateTimeOffset EventDate,
        bool Organized);

    internal static async Task<MonthlyRecapResponse> BuildAsync(
        IApplicationDbContext dbContext,
        Guid userId,
        DateOnly month,
        CancellationToken cancellationToken)
    {
        var played = await LoadPlayedAsync(
            dbContext, userId, LeaderboardCalculator.StartOf(month), LeaderboardCalculator.StartOf(month.AddMonths(1)), cancellationToken);
        var previous = await LoadPlayedAsync(
            dbContext, userId, LeaderboardCalculator.StartOf(month.AddMonths(-1)), LeaderboardCalculator.StartOf(month), cancellationToken);
        var eventIds = played.Select(item => item.EventId).ToList();

        var topSports = await RankSportsAsync(dbContext, played, cancellationToken);
        var topDistrict = TopDistrict(played);
        var busiestWeekday = BusiestWeekday(played);

        var (teammatesCount, partners) = await LoadPartnersAsync(
            dbContext, userId, eventIds, cancellationToken);
        var topPartner = partners.FirstOrDefault();

        var medals = await LoadMedalsAsync(
            dbContext, userId, LeaderboardCalculator.StartOf(month), LeaderboardCalculator.StartOf(month.AddMonths(1)), cancellationToken);

        var crown = await dbContext.DistrictLeaders.AsNoTracking()
            .Where(leader => leader.UserId == userId && leader.Month == month && leader.CrownedAt != null)
            .Select(leader => leader.District)
            .FirstOrDefaultAsync(cancellationToken);

        return new MonthlyRecapResponse(
            month.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture),
            played.Count >= MinEvents,
            played.Count,
            previous.Count,
            played.Sum(item => item.Minutes),
            topSports.Count,
            topSports.Take(3).ToList(),
            teammatesCount,
            topPartner,
            topDistrict,
            played.Select(item => item.District).Where(district => district is not null).Distinct().Count(),
            busiestWeekday,
            medals,
            crown);
    }

    /// <summary>Yıllık özet bu günden (Aralık) itibaren o yıl için açılır.</summary>
    internal const int YearlyRevealDay = 15;

    /// <summary>15 Aralık'tan önce geçen yıl, sonra bu yıl.</summary>
    internal static int DefaultYear(DateTimeOffset istanbulNow) =>
        istanbulNow is { Month: 12, Day: >= YearlyRevealDay } ? istanbulNow.Year : istanbulNow.Year - 1;

    internal static async Task<YearlyRecapResponse> BuildYearAsync(
        IApplicationDbContext dbContext,
        Guid userId,
        int year,
        CancellationToken cancellationToken)
    {
        var january = new DateOnly(year, 1, 1);
        var windowStart = LeaderboardCalculator.StartOf(january);
        var windowEnd = LeaderboardCalculator.StartOf(january.AddYears(1));

        var played = await LoadPlayedAsync(dbContext, userId, windowStart, windowEnd, cancellationToken);
        var topSports = await RankSportsAsync(dbContext, played, cancellationToken);
        var (teammatesCount, partners) = await LoadPartnersAsync(
            dbContext, userId, played.Select(item => item.EventId).ToList(), cancellationToken);
        var medals = await LoadMedalsAsync(dbContext, userId, windowStart, windowEnd, cancellationToken);

        var crowns = (await dbContext.DistrictLeaders.AsNoTracking()
                .Where(leader => leader.UserId == userId
                    && leader.CrownedAt != null
                    && leader.Month >= january
                    && leader.Month < january.AddYears(1))
                .Select(leader => new { leader.Month, leader.District })
                .ToListAsync(cancellationToken))
            .OrderBy(crown => crown.Month)
            .Select(crown => new YearlyRecapCrownResponse(crown.Month.Month, crown.District))
            .ToList();

        var monthlyCounts = new int[12];
        foreach (var item in played)
        {
            monthlyCounts[item.EventDate.ToOffset(StreakQueries.IstanbulOffset).Month - 1]++;
        }

        int? busiestMonth = played.Count == 0
            ? null
            : Array.IndexOf(monthlyCounts, monthlyCounts.Max()) + 1;

        var busiestWeekday = BusiestWeekday(played);
        var eventsOrganized = played.Count(item => item.Organized);
        var districtsCount = played.Select(item => item.District).Where(district => district is not null).Distinct().Count();
        var topPartnerTogether = partners.FirstOrDefault()?.EventsTogether ?? 0;

        return new YearlyRecapResponse(
            year,
            played.Count >= MinEvents,
            played.Count,
            eventsOrganized,
            played.Sum(item => item.Minutes),
            topSports.Count,
            topSports.Take(5).ToList(),
            teammatesCount,
            partners,
            TopDistrict(played),
            districtsCount,
            busiestWeekday,
            busiestMonth,
            monthlyCounts,
            LongestWeeklyStreak(played),
            medals,
            crowns,
            Persona(played.Count, eventsOrganized, topSports.Count, teammatesCount, topPartnerTogether, played));
    }

    /// <summary>Arka arkaya en az bir etkinlik olan en uzun hafta dizisi (Pazartesi başlangıçlı).</summary>
    internal static int LongestWeeklyStreak(IEnumerable<Played> played)
    {
        var weeks = played
            .Select(item => StreakQueries.WeekStartOf(item.EventDate))
            .Distinct()
            .Order()
            .ToList();

        var longest = 0;
        var run = 0;
        DateOnly? previous = null;
        foreach (var week in weeks)
        {
            run = previous is { } last && last.AddDays(7) == week ? run + 1 : 1;
            longest = Math.Max(longest, run);
            previous = week;
        }

        return longest;
    }

    /// <summary>
    /// Paylaşılan kartın kişilik etiketi; ilk uyan kural kazanır. Eşikler küçük
    /// kullanıcı tabanına göre: yılda bir avuç etkinlik bile bir etiket alsın.
    /// </summary>
    internal static string Persona(
        int events,
        int organized,
        int sportsCount,
        int teammates,
        int topPartnerTogether,
        IReadOnlyCollection<Played> played)
    {
        if (events >= 40)
        {
            return "unstoppable";
        }

        if (organized >= 3 && organized * 3 >= events)
        {
            return "captain";
        }

        if (sportsCount >= 4)
        {
            return "explorer";
        }

        if (topPartnerTogether >= 5 || teammates >= 20)
        {
            return "teamPlayer";
        }

        var weekend = played.Count(item =>
            item.EventDate.ToOffset(StreakQueries.IstanbulOffset).DayOfWeek
                is DayOfWeek.Saturday or DayOfWeek.Sunday);
        if (events >= 3 && weekend * 2 > events)
        {
            return "weekendWarrior";
        }

        return "risingStar";
    }

    internal static async Task<List<MonthlyRecapSportResponse>> RankSportsAsync(
        IApplicationDbContext dbContext,
        IReadOnlyCollection<Played> played,
        CancellationToken cancellationToken)
    {
        var sportIds = played.Select(item => item.SportId).Distinct().ToList();
        var sports = await dbContext.Sports.AsNoTracking()
            .Where(sport => sportIds.Contains(sport.Id))
            .ToDictionaryAsync(sport => sport.Id, cancellationToken);

        return played
            .GroupBy(item => item.SportId)
            .Where(group => sports.ContainsKey(group.Key))
            .Select(group => new MonthlyRecapSportResponse(
                CatalogLocalization.Resolve(sports[group.Key].Name, sports[group.Key].NameEn),
                sports[group.Key].Slug,
                group.Count()))
            .OrderByDescending(item => item.EventsPlayed)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    internal static string? TopDistrict(IEnumerable<Played> played) =>
        played
            .Where(item => item.District is not null)
            .GroupBy(item => item.District!)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Key)
            .FirstOrDefault();

    internal static int? BusiestWeekday(IEnumerable<Played> played) =>
        played
            .GroupBy(item => item.EventDate.ToOffset(StreakQueries.IstanbulOffset).DayOfWeek)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Select(group => (int?)group.Key)
            .FirstOrDefault();

    internal static async Task<List<MonthlyRecapMedalResponse>> LoadMedalsAsync(
        IApplicationDbContext dbContext,
        Guid userId,
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        CancellationToken cancellationToken)
    {
        var medals = await (
                from participant in dbContext.ChallengeParticipants.AsNoTracking()
                join challenge in dbContext.Challenges.AsNoTracking()
                    on participant.ChallengeId equals challenge.Id
                where participant.UserId == userId
                    && participant.CompletedAt != null
                    && challenge.StartsAt >= windowStart
                    && challenge.StartsAt < windowEnd
                orderby challenge.SortOrder
                select new { challenge.Title, challenge.TitleEn, challenge.Icon })
            .ToListAsync(cancellationToken);

        return medals
            .Select(medal => new MonthlyRecapMedalResponse(
                CatalogLocalization.Resolve(medal.Title, medal.TitleEn), medal.Icon))
            .ToList();
    }

    /// <summary>Bu ay en az <see cref="MinEvents"/> etkinliği olan kullanıcılar (bildirim için).</summary>
    internal static async Task<List<Guid>> LoadEligibleUsersAsync(
        IApplicationDbContext dbContext,
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        CancellationToken cancellationToken)
    {

        var attended = await (
                from participant in dbContext.EventParticipants.AsNoTracking()
                join @event in dbContext.Events.AsNoTracking()
                    on participant.EventId equals @event.Id
                where participant.UserId != null
                    && participant.Status == ParticipantStatus.Attended
                    && @event.Status != EventStatus.Cancelled
                    && @event.EventDate >= windowStart
                    && @event.EventDate < windowEnd
                select new { UserId = participant.UserId!.Value, EventId = @event.Id })
            .ToListAsync(cancellationToken);

        var organized = await dbContext.Events.AsNoTracking()
            .Where(@event => @event.Status == EventStatus.Completed
                && @event.EventDate >= windowStart
                && @event.EventDate < windowEnd)
            .Select(@event => new { UserId = @event.OrganizerUserId, EventId = @event.Id })
            .ToListAsync(cancellationToken);

        return attended.Concat(organized)
            .Distinct()
            .GroupBy(item => item.UserId)
            .Where(group => group.Count() >= MinEvents)
            .Select(group => group.Key)
            .ToList();
    }

    internal static async Task<List<Played>> LoadPlayedAsync(
        IApplicationDbContext dbContext,
        Guid userId,
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        CancellationToken cancellationToken)
    {

        var attended = await (
                from participant in dbContext.EventParticipants.AsNoTracking()
                join @event in dbContext.Events.AsNoTracking()
                    on participant.EventId equals @event.Id
                where participant.UserId == userId
                    && participant.Status == ParticipantStatus.Attended
                    && @event.Status != EventStatus.Cancelled
                    && @event.EventDate >= windowStart
                    && @event.EventDate < windowEnd
                select new { @event.Id, @event.SportId, @event.DurationMinutes, @event.District, @event.EventDate, Organized = false })
            .ToListAsync(cancellationToken);

        var organized = await dbContext.Events.AsNoTracking()
            .Where(@event => @event.OrganizerUserId == userId
                && @event.Status == EventStatus.Completed
                && @event.EventDate >= windowStart
                && @event.EventDate < windowEnd)
            .Select(@event => new { @event.Id, @event.SportId, @event.DurationMinutes, @event.District, @event.EventDate, Organized = true })
            .ToListAsync(cancellationToken);

        // Organize edilen önce gelsin ki aynı etkinlikte hem katılımcı hem organizatörse
        // "düzenledi" bilgisi kaybolmasın.
        return organized.Concat(attended)
            .DistinctBy(item => item.Id)
            .Select(item => new Played(
                item.Id,
                item.SportId,
                item.DurationMinutes ?? Domain.Events.Event.DefaultDurationMinutes,
                item.District,
                item.EventDate,
                item.Organized))
            .ToList();
    }

    /// <summary>
    /// Aynı maçta "geldi" işaretlenen diğer oyuncular ve organizatör. En çok
    /// birlikte oynanan kişi yalnızca herkese açık profili olan ve sıralamalarda
    /// görünmeyi kapatmamış biri olabilir; ad paylaşılan görsele çıkıyor.
    /// </summary>
    internal static async Task<(int Count, List<MonthlyRecapPartnerResponse> Top)> LoadPartnersAsync(
        IApplicationDbContext dbContext,
        Guid userId,
        IReadOnlyCollection<Guid> eventIds,
        CancellationToken cancellationToken)
    {
        if (eventIds.Count == 0)
        {
            return (0, []);
        }

        var attendees = await dbContext.EventParticipants.AsNoTracking()
            .Where(participant => eventIds.Contains(participant.EventId)
                && participant.UserId != null
                && participant.UserId != userId
                && participant.Status == ParticipantStatus.Attended)
            .Select(participant => new { UserId = participant.UserId!.Value, participant.EventId })
            .ToListAsync(cancellationToken);

        var organizers = await dbContext.Events.AsNoTracking()
            .Where(@event => eventIds.Contains(@event.Id) && @event.OrganizerUserId != userId)
            .Select(@event => new { UserId = @event.OrganizerUserId, EventId = @event.Id })
            .ToListAsync(cancellationToken);

        var counts = attendees.Concat(organizers)
            .Distinct()
            .GroupBy(item => item.UserId)
            .ToDictionary(group => group.Key, group => group.Count());

        if (counts.Count == 0)
        {
            return (0, []);
        }

        var candidateIds = counts.Keys.ToList();
        var profiles = await (
                from user in dbContext.Users.AsNoTracking()
                join profile in dbContext.UserProfiles.AsNoTracking()
                    on user.Id equals profile.UserId
                where candidateIds.Contains(user.Id)
                    && user.Status == UserStatus.Active
                    && profile.IsProfilePublic
                    && profile.ShowOnLeaderboards
                select profile)
            .ToListAsync(cancellationToken);

        var top = profiles
            .Select(profile => new { Profile = profile, Count = counts[profile.UserId] })
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Profile.Username, StringComparer.Ordinal)
            .Take(3)
            .Select(item => new MonthlyRecapPartnerResponse(
                item.Profile.UserId,
                item.Profile.Username,
                string.Join(' ', new[] { item.Profile.FirstName, item.Profile.LastName }
                    .Where(part => !string.IsNullOrWhiteSpace(part))),
                item.Profile.ProfileImageUrl,
                item.Count))
            .ToList();

        return (counts.Count, top);
    }
}
