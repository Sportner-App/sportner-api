using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Identity.UserProfiles.Streaks;

public sealed record StreakResponse(
    int CurrentStreak,
    int LongestStreak,
    int WeeklyGoal,
    int ThisWeekCount,
    IReadOnlyList<StreakWeekResponse> RecentWeeks);

/// <param name="WeekStart">Haftanın pazartesisi (İstanbul takvimi).</param>
public sealed record StreakWeekResponse(DateOnly WeekStart, int Count);

/// <summary>
/// Haftalık seri: arka arkaya en az bir maç yapılan hafta sayısı. Maç, yoklamada
/// "geldi" işaretlenen ya da organizatörü olunan tamamlanmış etkinlik. Seri
/// içinde bulunulan hafta henüz boşken bozulmaz; pazar gecesine kadar süre var.
/// Değer saklanmıyor, katılım kayıtlarından her istekte türetiliyor: yoklama
/// sonradan işaretlense de maç doğru haftaya düşüyor.
/// </summary>
internal static class StreakQueries
{
    internal const int RecentWeeksCount = 8;

    /// <summary>Türkiye 2016'dan beri yaz saati uygulamıyor; sabit UTC+3.</summary>
    private static readonly TimeSpan IstanbulOffset = TimeSpan.FromHours(3);

    internal static async Task<StreakResponse> BuildAsync(
        IApplicationDbContext dbContext,
        Guid userId,
        int weeklyGoal,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var attended = await (
                from participant in dbContext.EventParticipants.AsNoTracking()
                join @event in dbContext.Events.AsNoTracking()
                    on participant.EventId equals @event.Id
                where participant.UserId == userId
                    && participant.Status == ParticipantStatus.Attended
                    && @event.Status != EventStatus.Cancelled
                select new { @event.Id, @event.EventDate })
            .ToListAsync(cancellationToken);

        var organized = await dbContext.Events.AsNoTracking()
            .Where(@event => @event.OrganizerUserId == userId
                && @event.Status == EventStatus.Completed)
            .Select(@event => new { @event.Id, @event.EventDate })
            .ToListAsync(cancellationToken);

        var eventDates = attended
            .Concat(organized)
            .DistinctBy(item => item.Id)
            .Select(item => item.EventDate);

        return Calculate(eventDates, weeklyGoal, utcNow);
    }

    internal static StreakResponse Calculate(
        IEnumerable<DateTimeOffset> eventDates,
        int weeklyGoal,
        DateTimeOffset utcNow)
    {
        var currentWeek = WeekStartOf(utcNow);
        var countsByWeek = eventDates
            .Select(WeekStartOf)
            .Where(week => week <= currentWeek)
            .GroupBy(week => week)
            .ToDictionary(group => group.Key, group => group.Count());

        var thisWeekCount = countsByWeek.GetValueOrDefault(currentWeek);

        // Bu hafta boşsa seri geçen haftadan sayılmaya başlar.
        var cursor = thisWeekCount > 0 ? currentWeek : currentWeek.AddDays(-7);
        var currentStreak = 0;
        while (countsByWeek.ContainsKey(cursor))
        {
            currentStreak++;
            cursor = cursor.AddDays(-7);
        }

        var longestStreak = 0;
        var run = 0;
        DateOnly? previous = null;
        foreach (var week in countsByWeek.Keys.Order())
        {
            run = previous is { } last && week == last.AddDays(7) ? run + 1 : 1;
            longestStreak = Math.Max(longestStreak, run);
            previous = week;
        }

        var recentWeeks = Enumerable.Range(0, RecentWeeksCount)
            .Select(index => currentWeek.AddDays(-7 * (RecentWeeksCount - 1 - index)))
            .Select(week => new StreakWeekResponse(week, countsByWeek.GetValueOrDefault(week)))
            .ToList();

        return new StreakResponse(
            currentStreak,
            Math.Max(longestStreak, currentStreak),
            weeklyGoal,
            thisWeekCount,
            recentWeeks);
    }

    private static DateOnly WeekStartOf(DateTimeOffset moment)
    {
        var local = DateOnly.FromDateTime(moment.ToOffset(IstanbulOffset).DateTime);
        var daysSinceMonday = ((int)local.DayOfWeek + 6) % 7;
        return local.AddDays(-daysSinceMonday);
    }
}
