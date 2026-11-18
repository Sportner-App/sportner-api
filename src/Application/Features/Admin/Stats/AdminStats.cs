using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Feedback;

namespace Sportner.Application.Features.Admin.Stats;

public sealed record AdminUserStats(
    int Total, int New24h, int New7d, int New30d, int Active24h, int Active7d, int Active30d);

public sealed record AdminEventStats(int Created7d, int Upcoming, int Completed7d, int Cancelled7d);

public sealed record AdminClubStats(int Total, int New7d);

public sealed record AdminPostStats(int New7d);

public sealed record AdminPendingStats(int Reports, int Feedback);

public sealed record AdminSignupDay(string Date, int Count);

public sealed record AdminStats(
    AdminUserStats Users,
    AdminEventStats Events,
    AdminClubStats Clubs,
    AdminPostStats Posts,
    AdminPendingStats Pending,
    IReadOnlyList<AdminSignupDay> SignupsByDay);

public sealed record GetAdminStatsQuery : IQuery<AdminStats>;

/// <summary>
/// Panel özeti. Test hesapları ve onların içerikleri sayılmaz. Her sayı ayrı CountAsync ile alınır;
/// günlük kayıt grafiği için yalnızca son 30 günün kayıt zamanları çekilir.
/// </summary>
internal sealed class GetAdminStatsQueryHandler : IQueryHandler<GetAdminStatsQuery, AdminStats>
{
    internal const int SignupDays = 30;

    // Europe/Istanbul yıl boyu UTC+3 (yaz saati uygulaması yok).
    private static readonly TimeSpan IstanbulOffset = TimeSpan.FromHours(3);

    private readonly IApplicationDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public GetAdminStatsQueryHandler(IApplicationDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result<AdminStats>> Handle(GetAdminStatsQuery request, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var d1 = now.AddDays(-1);
        var d7 = now.AddDays(-7);
        var d30 = now.AddDays(-30);

        var users = _dbContext.Users.AsNoTracking().Where(user => !user.IsTestAccount);
        var events = _dbContext.Events.AsNoTracking()
            .Where(@event => !_dbContext.Users.Any(user => user.Id == @event.OrganizerUserId && user.IsTestAccount));
        var clubs = _dbContext.Organizations.AsNoTracking()
            .Where(club => !_dbContext.Users.Any(user => user.Id == club.FounderUserId && user.IsTestAccount));
        var posts = _dbContext.Posts.AsNoTracking()
            .Where(post => !_dbContext.Users.Any(user => user.Id == post.UserId && user.IsTestAccount));

        var userStats = new AdminUserStats(
            await users.CountAsync(cancellationToken),
            await users.CountAsync(user => user.CreatedAt >= d1, cancellationToken),
            await users.CountAsync(user => user.CreatedAt >= d7, cancellationToken),
            await users.CountAsync(user => user.CreatedAt >= d30, cancellationToken),
            await users.CountAsync(user => user.LastSeenAt >= d1, cancellationToken),
            await users.CountAsync(user => user.LastSeenAt >= d7, cancellationToken),
            await users.CountAsync(user => user.LastSeenAt >= d30, cancellationToken));

        // Tamamlanma / iptal için ayrı bir zaman alanı yok; duruma geçiş UpdatedAt'e yazılır.
        var eventStats = new AdminEventStats(
            await events.CountAsync(@event => @event.CreatedAt >= d7, cancellationToken),
            await events.CountAsync(
                @event => (@event.Status == EventStatus.Published || @event.Status == EventStatus.Full)
                    && @event.EventDate > now,
                cancellationToken),
            await events.CountAsync(
                @event => @event.Status == EventStatus.Completed && @event.UpdatedAt >= d7, cancellationToken),
            await events.CountAsync(
                @event => @event.Status == EventStatus.Cancelled && @event.UpdatedAt >= d7, cancellationToken));

        var clubStats = new AdminClubStats(
            await clubs.CountAsync(cancellationToken),
            await clubs.CountAsync(club => club.CreatedAt >= d7, cancellationToken));

        var postStats = new AdminPostStats(await posts.CountAsync(post => post.CreatedAt >= d7, cancellationToken));

        var pending = new AdminPendingStats(
            await _dbContext.Reports.CountAsync(
                report => report.Status == ReportStatus.Pending || report.Status == ReportStatus.UnderReview,
                cancellationToken),
            await _dbContext.AppFeedbacks.CountAsync(
                feedback => feedback.Status == AppFeedbackStatus.New, cancellationToken));

        var today = now.ToOffset(IstanbulOffset).Date;
        var firstDay = today.AddDays(-(SignupDays - 1));
        // Npgsql timestamptz parametresinde yalnızca UTC offset'i kabul ediyor.
        var firstDayUtc = new DateTimeOffset(firstDay, IstanbulOffset).ToUniversalTime();
        var signups = await users
            .Where(user => user.CreatedAt >= firstDayUtc)
            .Select(user => user.CreatedAt)
            .ToListAsync(cancellationToken);
        var perDay = signups
            .GroupBy(createdAt => createdAt.ToOffset(IstanbulOffset).Date)
            .ToDictionary(group => group.Key, group => group.Count());
        var series = Enumerable.Range(0, SignupDays)
            .Select(offset => firstDay.AddDays(offset))
            .Select(day => new AdminSignupDay(day.ToString("yyyy-MM-dd"), perDay.GetValueOrDefault(day)))
            .ToList();

        return Result<AdminStats>.Success(
            new AdminStats(userStats, eventStats, clubStats, postStats, pending, series));
    }
}
