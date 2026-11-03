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
using Sportner.Application.Features.Recaps;
using Sportner.Domain.Common.Enums;
using Sportner.Localization.Resources;

namespace Sportner.Application.BackgroundJobs;

/// <summary>
/// Saatlik özet işi. Yıllık: 15–21 Aralık arasında (10:00–21:59) bu yıl en az
/// <see cref="RecapCalculator.MinEvents"/> etkinliği olanlara "yılın özeti hazır".
/// Aylık: ayın 3–7'si arasında, 10:00–21:59 İstanbul'da, geçen ay
/// en az <see cref="RecapCalculator.MinEvents"/> maçı olanlara "özetin hazır"
/// bildirimi gider. 3'ü beklenir ki ay sonu maçlarının yoklaması girilsin ve semt
/// taçları (<see cref="DistrictLeaderDispatcher"/>) verilmiş olsun.
/// </summary>
internal sealed class MonthlyRecapDispatcher : IMonthlyRecapDispatcher
{
    private const int FirstNotifyHour = 10;
    private const int LastNotifyHour = 21;
    private const int FirstDay = 3;
    private const int LastDay = 7;
    private const int YearlyLastDay = 21;

    private readonly IApplicationDbContext _dbContext;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MonthlyRecapDispatcher> _logger;

    public MonthlyRecapDispatcher(
        IApplicationDbContext dbContext,
        INotificationPublisher notificationPublisher,
        TimeProvider timeProvider,
        ILogger<MonthlyRecapDispatcher> logger)
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

        if (local is { Month: 12, Day: >= RecapCalculator.YearlyRevealDay and <= YearlyLastDay })
        {
            await DispatchYearlyAsync(local.Year, cancellationToken);
        }

        if (local.Day is < FirstDay or > LastDay)
        {
            return;
        }

        var current = LeaderboardCalculator.MonthOf(utcNow);
        var month = current.AddMonths(-1);
        var monthStart = LeaderboardCalculator.StartOf(current);

        var eligible = await RecapCalculator.LoadEligibleUsersAsync(
            _dbContext,
            LeaderboardCalculator.StartOf(month),
            monthStart,
            cancellationToken);

        var alreadyNotified = (await _dbContext.Notifications.AsNoTracking()
                .Where(notification =>
                    notification.NotificationType == NotificationType.MonthlyRecapReady
                    && notification.CreatedAt >= monthStart)
                .Select(notification => notification.RecipientUserId)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var active = (await _dbContext.Users.AsNoTracking()
                .Where(user => eligible.Contains(user.Id) && user.Status == UserStatus.Active)
                .Select(user => user.Id)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var recipients = eligible
            .Where(userId => active.Contains(userId) && !alreadyNotified.Contains(userId))
            .ToList();

        var languages = await NotificationActor.ResolveRecipientLanguagesAsync(
            _dbContext, recipients, cancellationToken);

        foreach (var userId in recipients)
        {
            var recap = await RecapCalculator.BuildAsync(_dbContext, userId, month, cancellationToken);
            var language = languages.GetValueOrDefault(userId);
            var hours = Math.Round(recap.MinutesPlayed / 60m, 1, MidpointRounding.AwayFromZero);

            await _notificationPublisher.PublishAsync(
                userId,
                NotificationType.MonthlyRecapReady,
                NotificationActor.Format(
                    language,
                    nameof(NotificationsResource.MonthlyRecapReady_Title),
                    month.ToDateTime(TimeOnly.MinValue).ToString("MMMM", language.ToCultureInfo())),
                NotificationActor.Format(
                    language,
                    nameof(NotificationsResource.MonthlyRecapReady_Body),
                    recap.EventsPlayed,
                    hours.ToString("0.#", language.ToCultureInfo())),
                NotificationEntityType.MonthlyRecap,
                entityId: null,
                actorUserId: null,
                cancellationToken);
        }

        _logger.LogInformation(
            "Monthly recap dispatcher: {Sent} recap notifications for {Month}.",
            recipients.Count,
            month.ToString("yyyy-MM", CultureInfo.InvariantCulture));
    }

    private async Task DispatchYearlyAsync(int year, CancellationToken cancellationToken)
    {
        var january = new DateOnly(year, 1, 1);
        var windowStart = LeaderboardCalculator.StartOf(january);
        var windowEnd = LeaderboardCalculator.StartOf(january.AddYears(1));
        var revealStart = LeaderboardCalculator.StartOf(new DateOnly(year, 12, 1));

        var eligible = await RecapCalculator.LoadEligibleUsersAsync(
            _dbContext, windowStart, windowEnd, cancellationToken);

        var alreadyNotified = (await _dbContext.Notifications.AsNoTracking()
                .Where(notification =>
                    notification.NotificationType == NotificationType.YearlyRecapReady
                    && notification.CreatedAt >= revealStart)
                .Select(notification => notification.RecipientUserId)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var active = (await _dbContext.Users.AsNoTracking()
                .Where(user => eligible.Contains(user.Id) && user.Status == UserStatus.Active)
                .Select(user => user.Id)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var recipients = eligible
            .Where(userId => active.Contains(userId) && !alreadyNotified.Contains(userId))
            .ToList();

        var languages = await NotificationActor.ResolveRecipientLanguagesAsync(
            _dbContext, recipients, cancellationToken);

        foreach (var userId in recipients)
        {
            var recap = await RecapCalculator.BuildYearAsync(_dbContext, userId, year, cancellationToken);
            var language = languages.GetValueOrDefault(userId);
            var hours = Math.Round(recap.MinutesPlayed / 60m, 0, MidpointRounding.AwayFromZero);

            await _notificationPublisher.PublishAsync(
                userId,
                NotificationType.YearlyRecapReady,
                NotificationActor.Format(
                    language, nameof(NotificationsResource.YearlyRecapReady_Title), year),
                NotificationActor.Format(
                    language,
                    nameof(NotificationsResource.YearlyRecapReady_Body),
                    recap.EventsPlayed,
                    hours.ToString("0", language.ToCultureInfo())),
                NotificationEntityType.YearlyRecap,
                entityId: null,
                actorUserId: null,
                cancellationToken);
        }

        _logger.LogInformation(
            "Monthly recap dispatcher: {Sent} yearly recap notifications for {Year}.", recipients.Count, year);
    }
}
