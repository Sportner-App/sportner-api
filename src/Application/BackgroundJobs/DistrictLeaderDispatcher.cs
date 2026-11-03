using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sportner.Application.Abstractions.BackgroundJobs;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Localization;
using Sportner.Application.Features.Identity.UserProfiles.Streaks;
using Sportner.Application.Features.Leaderboards;
using Sportner.Application.Features.Notifications;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Leaderboards;
using Sportner.Localization.Resources;

namespace Sportner.Application.BackgroundJobs;

/// <summary>
/// Saatlik semt lideri işi (10:00–21:59 İstanbul; dışında hiçbir şey yapmaz,
/// bildirim kaçmasın diye durum da güncellenmez):
/// 1. Bu ayın her semtinde en az <see cref="LeaderboardCalculator.MinPlayers"/> oyuncu
///    varsa lider tutulur; lider değişince yeni lidere "1. sıradasın" gider.
///    Geçilen kişiye bildirim gitmez (küçük tabanda çok sık tetiklenir).
/// 2. Ayın 3'ünden itibaren geçen ayın liderleri kesinleşip taç verilir. 3 gün
///    bekleme, ay sonundaki maçların yoklamasına süre tanır.
/// </summary>
internal sealed class DistrictLeaderDispatcher : IDistrictLeaderDispatcher
{
    private const int FirstNotifyHour = 10;
    private const int LastNotifyHour = 21;
    private const int CrownOnDay = 3;

    private readonly IApplicationDbContext _dbContext;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DistrictLeaderDispatcher> _logger;

    public DistrictLeaderDispatcher(
        IApplicationDbContext dbContext,
        INotificationPublisher notificationPublisher,
        TimeProvider timeProvider,
        ILogger<DistrictLeaderDispatcher> logger)
    {
        _dbContext = dbContext;
        _notificationPublisher = notificationPublisher;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task DispatchAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = _timeProvider.GetUtcNow();
        var local = utcNow.ToOffset(StreakQueries.IstanbulOffset);
        if (local.Hour is < FirstNotifyHour or > LastNotifyHour)
        {
            return;
        }

        var month = LeaderboardCalculator.MonthOf(utcNow);
        var changed = await TrackAsync(month, crown: false, utcNow, cancellationToken);

        var crowned = 0;
        if (local.Day >= CrownOnDay)
        {
            crowned = await TrackAsync(month.AddMonths(-1), crown: true, utcNow, cancellationToken);
        }

        _logger.LogInformation(
            "District leader dispatcher: {Changed} new leaders, {Crowned} crowns.", changed, crowned);
    }

    private async Task<int> TrackAsync(
        DateOnly month,
        bool crown,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.DistrictLeaders
            .Where(leader => leader.Month == month)
            .ToListAsync(cancellationToken);

        // Geçen ayın tüm taçları verildiyse yeniden hesaplamaya gerek yok.
        if (crown && existing.Count > 0 && existing.All(leader => leader.CrownedAt is not null))
        {
            return 0;
        }

        var rows = await LeaderboardCalculator.LoadRowsAsync(
            _dbContext,
            city: null,
            district: null,
            sportId: null,
            LeaderboardCalculator.StartOf(month),
            LeaderboardCalculator.StartOf(month.AddMonths(1)),
            cancellationToken);

        var notify = new List<DistrictLeader>();
        foreach (var district in rows.GroupBy(row => (row.City, row.District)))
        {
            var standings = LeaderboardCalculator.Rank(district);
            if (standings.Count < LeaderboardCalculator.MinPlayers)
            {
                continue;
            }

            var top = standings[0];
            var record = existing.FirstOrDefault(leader =>
                leader.City == district.Key.City && leader.District == district.Key.District);

            if (record is null)
            {
                record = DistrictLeader.Create(
                    district.Key.City, district.Key.District, month, top.UserId, top.EventCount, utcNow);
                _dbContext.DistrictLeaders.Add(record);
                existing.Add(record);
                notify.Add(record);
            }
            else if (record.CrownedAt is null && record.UpdateLeader(top.UserId, top.EventCount, utcNow))
            {
                notify.Add(record);
            }

            if (crown && record.CrownedAt is null)
            {
                record.Crown(utcNow);
                if (!notify.Contains(record))
                {
                    notify.Add(record);
                }
            }
        }

        if (notify.Count == 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return 0;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var languages = await NotificationActor.ResolveRecipientLanguagesAsync(
            _dbContext, notify.Select(leader => leader.UserId).Distinct().ToList(), cancellationToken);

        foreach (var leader in notify)
        {
            var language = languages.GetValueOrDefault(leader.UserId);
            var (title, body) = crown
                ? (
                    NotificationActor.Format(
                        language,
                        nameof(NotificationsResource.DistrictCrowned_Title),
                        MonthName(leader.Month, language),
                        leader.District),
                    NotificationActor.Format(
                        language, nameof(NotificationsResource.DistrictCrowned_Body), leader.EventCount))
                : (
                    NotificationActor.Format(
                        language, nameof(NotificationsResource.DistrictLeaderTaken_Title), leader.District),
                    NotificationActor.Format(
                        language, nameof(NotificationsResource.DistrictLeaderTaken_Body), leader.District));

            await _notificationPublisher.PublishAsync(
                leader.UserId,
                crown ? NotificationType.DistrictCrowned : NotificationType.DistrictLeaderTaken,
                title,
                body,
                NotificationEntityType.DistrictLeader,
                leader.Id,
                actorUserId: null,
                cancellationToken);
        }

        return notify.Count;
    }

    private static string MonthName(DateOnly month, Language language) =>
        month.ToDateTime(TimeOnly.MinValue).ToString("MMMM", language.ToCultureInfo());
}
