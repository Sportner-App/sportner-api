using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Features.Identity.UserProfiles.Streaks;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Leaderboards;

/// <summary>
/// Semt sıralaması: bir semtte (şehir + semt) oynanan maç sayısı. Karne, seri ve
/// challenge'larla aynı kural: yoklamada "geldi" işaretlenen ya da organizatörü
/// olunan tamamlanmış etkinlikler. Gizli profiller ve "sıralamada görünme"
/// diyenler listeye girmez.
/// </summary>
internal static class LeaderboardCalculator
{
    /// <summary>Bu kadar oyuncu yoksa liste ve taç yok; yerine davet gösterilir.</summary>
    internal const int MinPlayers = 3;

    internal sealed record Row(Guid UserId, string City, string District, Guid EventId, DateTimeOffset EventDate);

    internal sealed record Standing(Guid UserId, int Rank, int EventCount);

    internal static DateOnly MonthOf(DateTimeOffset utcNow)
    {
        var local = utcNow.ToOffset(StreakQueries.IstanbulOffset);
        return new DateOnly(local.Year, local.Month, 1);
    }

    /// <remarks>UTC'ye çevrilir: Npgsql timestamptz'ye yalnızca sıfır offset yazar/karşılaştırır.</remarks>
    internal static DateTimeOffset StartOf(DateOnly month) =>
        new DateTimeOffset(month.ToDateTime(TimeOnly.MinValue), StreakQueries.IstanbulOffset)
            .ToUniversalTime();

    /// <summary>Semt/şehir filtreleri null ise tüm semtler gelir (iş için).</summary>
    internal static async Task<List<Row>> LoadRowsAsync(
        IApplicationDbContext dbContext,
        string? city,
        string? district,
        Guid? sportId,
        DateTimeOffset? windowStart,
        DateTimeOffset? windowEnd,
        CancellationToken cancellationToken)
    {
        var events = dbContext.Events.AsNoTracking()
            .Where(@event => @event.District != null && @event.City != null);

        if (city is not null)
        {
            events = events.Where(@event => @event.City == city);
        }

        if (district is not null)
        {
            events = events.Where(@event => @event.District == district);
        }

        if (sportId is { } sport)
        {
            events = events.Where(@event => @event.SportId == sport);
        }

        if (windowStart is { } start)
        {
            events = events.Where(@event => @event.EventDate >= start);
        }

        if (windowEnd is { } end)
        {
            events = events.Where(@event => @event.EventDate < end);
        }

        var attended = await (
                from participant in dbContext.EventParticipants.AsNoTracking()
                join @event in events on participant.EventId equals @event.Id
                where participant.UserId != null
                    && participant.Status == ParticipantStatus.Attended
                    && @event.Status != EventStatus.Cancelled
                select new Row(
                    participant.UserId!.Value, @event.City!, @event.District!, @event.Id, @event.EventDate))
            .ToListAsync(cancellationToken);

        var organized = await events
            .Where(@event => @event.Status == EventStatus.Completed)
            .Select(@event => new Row(
                @event.OrganizerUserId, @event.City!, @event.District!, @event.Id, @event.EventDate))
            .ToListAsync(cancellationToken);

        var rows = attended.Concat(organized)
            .DistinctBy(row => (row.UserId, row.EventId))
            .ToList();

        var userIds = rows.Select(row => row.UserId).Distinct().ToList();
        var eligible = (await (
                    from user in dbContext.Users.AsNoTracking()
                    join profile in dbContext.UserProfiles.AsNoTracking()
                        on user.Id equals profile.UserId
                    where userIds.Contains(user.Id)
                        && user.Status == UserStatus.Active
                        && profile.IsProfilePublic
                        && profile.ShowOnLeaderboards
                    select user.Id)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        return rows.Where(row => eligible.Contains(row.UserId)).ToList();
    }

    /// <summary>
    /// Çok maç önde; eşitlikte o sayıya önce ulaşan (son maçı daha erken olan) önde.
    /// </summary>
    internal static List<Standing> Rank(IEnumerable<Row> rows) =>
        rows.GroupBy(row => row.UserId)
            .Select(group => new
            {
                UserId = group.Key,
                Count = group.Count(),
                Reached = group.Max(row => row.EventDate)
            })
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Reached)
            .Select((item, index) => new Standing(item.UserId, index + 1, item.Count))
            .ToList();
}
