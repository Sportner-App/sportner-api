using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sportner.Application.Abstractions.BackgroundJobs;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Localization;
using Sportner.Application.Features.Challenges;
using Sportner.Application.Features.Identity.UserProfiles.Streaks;
using Sportner.Application.Features.Notifications;
using Sportner.Domain.Challenges;
using Sportner.Domain.Common.Enums;
using Sportner.Localization.Resources;

namespace Sportner.Application.BackgroundJobs;

/// <summary>
/// Saatlik challenge işi:
/// 1. Ayın challenge'ları yoksa şablonlardan açılır.
/// 2. Hedefe ulaşan katılımcı tamamlandı işaretlenir, madalya bildirimi gider.
///    Bitişten sonra 3 gün daha bakılır: yoklama geç alınsa da madalya kaybolmaz.
/// 3. Ay başında "challenge'lar başladı", bitişe 5 gün kala "bitmek üzere" gider.
/// Bildirimler 10:00–21:59 (İstanbul) arasında gönderilir; dışında bir sonraki
/// uygun saate kalır.
/// </summary>
internal sealed class ChallengeDispatcher : IChallengeDispatcher
{
    /// <summary>Lansman ayı: süre kısaldığı için hedefler düşük tutuluyor.</summary>
    internal const string IntroMonth = "2026-11";

    private const int FirstNotifyHour = 10;
    private const int LastNotifyHour = 21;
    private static readonly TimeSpan CompletionGrace = TimeSpan.FromDays(3);
    private static readonly TimeSpan EndingSoonWindow = TimeSpan.FromDays(5);
    private static readonly TimeSpan LaunchWindow = TimeSpan.FromDays(7);

    private readonly IApplicationDbContext _dbContext;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ChallengeDispatcher> _logger;

    public ChallengeDispatcher(
        IApplicationDbContext dbContext,
        INotificationPublisher notificationPublisher,
        TimeProvider timeProvider,
        ILogger<ChallengeDispatcher> logger)
    {
        _dbContext = dbContext;
        _notificationPublisher = notificationPublisher;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task DispatchAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = _timeProvider.GetUtcNow();
        var created = await EnsureMonthlyChallengesAsync(utcNow, cancellationToken);

        var localHour = utcNow.ToOffset(StreakQueries.IstanbulOffset).Hour;
        if (localHour is < FirstNotifyHour or > LastNotifyHour)
        {
            _logger.LogInformation("Challenge dispatcher created {Created} challenges (quiet hours).", created);
            return;
        }

        var completed = await CompleteAsync(utcNow, cancellationToken);
        var launched = await SendLaunchAsync(utcNow, cancellationToken);
        var endingSoon = await SendEndingSoonAsync(utcNow, cancellationToken);

        _logger.LogInformation(
            "Challenge dispatcher: {Created} created, {Completed} completed, {Launched} launch and {EndingSoon} ending-soon notifications.",
            created, completed, launched, endingSoon);
    }

    internal static IReadOnlyList<Challenge> BuildMonthlyChallenges(DateTimeOffset utcNow)
    {
        var local = utcNow.ToOffset(StreakQueries.IstanbulOffset);
        var localStart = new DateTimeOffset(local.Year, local.Month, 1, 0, 0, 0, StreakQueries.IstanbulOffset);
        // Veritabanına UTC yazılmalı: Npgsql +03:00 offsetli değeri reddediyor.
        var startsAt = localStart.ToUniversalTime();
        var endsAt = localStart.AddMonths(1).ToUniversalTime();
        var key = localStart.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        var intro = key == IntroMonth;
        var monthTr = localStart.ToString("MMMM", CultureInfo.GetCultureInfo("tr-TR"));
        var monthEn = localStart.ToString("MMMM", CultureInfo.GetCultureInfo("en-US"));
        var play = intro ? 3 : 4;
        var explore = intro ? 2 : 3;

        return
        [
            Challenge.Create(
                $"{key}-play",
                $"{monthTr} Oyuncusu",
                $"{monthTr} ayında {play} etkinliğe katıl.",
                "medal",
                ChallengeMetric.EventsPlayed,
                play,
                startsAt,
                endsAt,
                sortOrder: 0,
                utcNow,
                titleEn: $"{monthEn} Player",
                descriptionEn: $"Play {play} events in {monthEn}."),
            Challenge.Create(
                $"{key}-explore",
                $"{monthTr} Kaşifi",
                $"{monthTr} ayında {explore} farklı spor dene.",
                "compass",
                ChallengeMetric.DistinctSports,
                explore,
                startsAt,
                endsAt,
                sortOrder: 1,
                utcNow,
                titleEn: $"{monthEn} Explorer",
                descriptionEn: $"Try {explore} different sports in {monthEn}."),
            Challenge.Create(
                $"{key}-organize",
                $"{monthTr} Kaptanı",
                $"{monthTr} ayında bir etkinlik düzenle ve tamamla.",
                "flag",
                ChallengeMetric.EventsOrganized,
                1,
                startsAt,
                endsAt,
                sortOrder: 2,
                utcNow,
                titleEn: $"{monthEn} Captain",
                descriptionEn: $"Organize and complete an event in {monthEn}.")
        ];
    }

    private async Task<int> EnsureMonthlyChallengesAsync(DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        var templates = BuildMonthlyChallenges(utcNow);
        var codes = templates.Select(challenge => challenge.Code).ToList();
        var existing = await _dbContext.Challenges.AsNoTracking()
            .Where(challenge => codes.Contains(challenge.Code))
            .Select(challenge => challenge.Code)
            .ToListAsync(cancellationToken);

        var missing = templates.Where(challenge => !existing.Contains(challenge.Code)).ToList();
        if (missing.Count > 0)
        {
            _dbContext.Challenges.AddRange(missing);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return missing.Count;
    }

    private async Task<int> CompleteAsync(DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        var graceStart = utcNow - CompletionGrace;
        var challenges = await _dbContext.Challenges.AsNoTracking()
            .Where(challenge =>
                challenge.IsActive && challenge.StartsAt <= utcNow && challenge.EndsAt > graceStart)
            .ToListAsync(cancellationToken);

        if (challenges.Count == 0)
        {
            return 0;
        }

        var byId = challenges.ToDictionary(challenge => challenge.Id);
        var challengeIds = byId.Keys.ToList();
        var pending = await _dbContext.ChallengeParticipants
            .Where(participant =>
                challengeIds.Contains(participant.ChallengeId) && participant.CompletedAt == null)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return 0;
        }

        var userIds = pending.Select(participant => participant.UserId).Distinct().ToList();
        var played = await ChallengeProgress.LoadPlayedAsync(
            _dbContext,
            userIds,
            challenges.Min(challenge => challenge.StartsAt),
            challenges.Max(challenge => challenge.EndsAt),
            cancellationToken);

        var newlyCompleted = pending
            .Where(participant =>
            {
                var challenge = byId[participant.ChallengeId];
                return ChallengeProgress.Compute(challenge, played[participant.UserId]) >= challenge.TargetValue
                    && participant.MarkCompleted(utcNow);
            })
            .ToList();

        if (newlyCompleted.Count == 0)
        {
            return 0;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var languages = await NotificationActor.ResolveRecipientLanguagesAsync(
            _dbContext, newlyCompleted.Select(participant => participant.UserId).Distinct().ToList(), cancellationToken);

        foreach (var participant in newlyCompleted)
        {
            var challenge = byId[participant.ChallengeId];
            var language = languages.GetValueOrDefault(participant.UserId);
            await _notificationPublisher.PublishAsync(
                participant.UserId,
                NotificationType.ChallengeCompleted,
                NotificationActor.Format(language, nameof(NotificationsResource.ChallengeCompleted_Title)),
                NotificationActor.Format(
                    language,
                    nameof(NotificationsResource.ChallengeCompleted_Body),
                    CatalogLocalization.Resolve(language, challenge.Title, challenge.TitleEn)),
                NotificationEntityType.Challenge,
                challenge.Id,
                actorUserId: null,
                cancellationToken);
        }

        return newlyCompleted.Count;
    }

    private async Task<int> SendLaunchAsync(DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        var launchedSince = utcNow - LaunchWindow;
        var lead = await _dbContext.Challenges.AsNoTracking()
            .Where(challenge =>
                challenge.IsActive
                && challenge.StartsAt <= utcNow
                && challenge.StartsAt > launchedSince
                && challenge.EndsAt > utcNow)
            .OrderBy(challenge => challenge.StartsAt)
            .ThenBy(challenge => challenge.SortOrder)
            .FirstOrDefaultAsync(cancellationToken);

        if (lead is null)
        {
            return 0;
        }

        var alreadyNotified = await _dbContext.Notifications.AsNoTracking()
            .Where(notification =>
                notification.NotificationType == NotificationType.ChallengesLaunched
                && notification.EntityId == lead.Id)
            .Select(notification => notification.RecipientUserId)
            .ToListAsync(cancellationToken);
        var cappedToday = await NotifiedTodayAsync(utcNow, cancellationToken);

        var recipients = await (
                from user in _dbContext.Users.AsNoTracking()
                join profile in _dbContext.UserProfiles.AsNoTracking()
                    on user.Id equals profile.UserId
                where user.Status == UserStatus.Active
                select user.Id)
            .ToListAsync(cancellationToken);
        recipients = recipients
            .Where(userId => !alreadyNotified.Contains(userId) && !cappedToday.Contains(userId))
            .ToList();

        var languages = await NotificationActor.ResolveRecipientLanguagesAsync(
            _dbContext, recipients, cancellationToken);
        var month = lead.StartsAt.ToOffset(StreakQueries.IstanbulOffset);

        foreach (var userId in recipients)
        {
            var language = languages.GetValueOrDefault(userId);
            await _notificationPublisher.PublishAsync(
                userId,
                NotificationType.ChallengesLaunched,
                NotificationActor.Format(
                    language,
                    nameof(NotificationsResource.ChallengesLaunched_Title),
                    month.ToString("MMMM", language.ToCultureInfo())),
                NotificationActor.Format(language, nameof(NotificationsResource.ChallengesLaunched_Body)),
                NotificationEntityType.Challenge,
                lead.Id,
                actorUserId: null,
                cancellationToken);
        }

        return recipients.Count;
    }

    private async Task<int> SendEndingSoonAsync(DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        var soon = utcNow + EndingSoonWindow;
        var challenges = await _dbContext.Challenges.AsNoTracking()
            .Where(challenge =>
                challenge.IsActive && challenge.EndsAt > utcNow && challenge.EndsAt <= soon)
            .ToListAsync(cancellationToken);

        if (challenges.Count == 0)
        {
            return 0;
        }

        var byId = challenges.ToDictionary(challenge => challenge.Id);
        var challengeIds = byId.Keys.ToList();
        var participants = await _dbContext.ChallengeParticipants.AsNoTracking()
            .Where(participant =>
                challengeIds.Contains(participant.ChallengeId) && participant.CompletedAt == null)
            .ToListAsync(cancellationToken);

        var alreadyNotified = (await _dbContext.Notifications.AsNoTracking()
                .Where(notification =>
                    notification.NotificationType == NotificationType.ChallengeEndingSoon
                    && notification.EntityId != null
                    && challengeIds.Contains(notification.EntityId.Value))
                .Select(notification => new { notification.RecipientUserId, notification.EntityId })
                .ToListAsync(cancellationToken))
            .Select(row => (row.RecipientUserId, row.EntityId!.Value))
            .ToHashSet();
        var cappedToday = await NotifiedTodayAsync(utcNow, cancellationToken);

        var userIds = participants.Select(participant => participant.UserId).Distinct().ToList();
        var played = await ChallengeProgress.LoadPlayedAsync(
            _dbContext,
            userIds,
            challenges.Min(challenge => challenge.StartsAt),
            challenges.Max(challenge => challenge.EndsAt),
            cancellationToken);
        var languages = await NotificationActor.ResolveRecipientLanguagesAsync(
            _dbContext, userIds, cancellationToken);

        var sent = 0;
        foreach (var group in participants.GroupBy(participant => participant.UserId))
        {
            if (cappedToday.Contains(group.Key))
            {
                continue;
            }

            // Kişi başı tek bildirim: madalyaya en yakın olan challenge.
            var pick = group
                .Where(participant => !alreadyNotified.Contains((group.Key, participant.ChallengeId)))
                .Select(participant =>
                {
                    var challenge = byId[participant.ChallengeId];
                    var remaining = challenge.TargetValue
                        - ChallengeProgress.Compute(challenge, played[group.Key]);
                    return (Challenge: challenge, Remaining: remaining);
                })
                .Where(item => item.Remaining > 0)
                .OrderBy(item => item.Remaining)
                .FirstOrDefault();

            if (pick.Challenge is null)
            {
                continue;
            }

            var language = languages.GetValueOrDefault(group.Key);
            var daysLeft = Math.Max(1, (int)Math.Ceiling((pick.Challenge.EndsAt - utcNow).TotalDays));
            await _notificationPublisher.PublishAsync(
                group.Key,
                NotificationType.ChallengeEndingSoon,
                NotificationActor.Format(
                    language,
                    nameof(NotificationsResource.ChallengeEndingSoon_Title),
                    CatalogLocalization.Resolve(language, pick.Challenge.Title, pick.Challenge.TitleEn)),
                NotificationActor.Format(
                    language,
                    nameof(NotificationsResource.ChallengeEndingSoon_Body),
                    daysLeft,
                    pick.Remaining),
                NotificationEntityType.Challenge,
                pick.Challenge.Id,
                actorUserId: null,
                cancellationToken);
            sent++;
        }

        return sent;
    }

    /// <summary>Günde-bir hatırlatıcı sınırı: bugün zaten hatırlatıcı alanlar.</summary>
    private async Task<HashSet<Guid>> NotifiedTodayAsync(DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        var localNow = utcNow.ToOffset(StreakQueries.IstanbulOffset);
        var todayStart = new DateTimeOffset(localNow.Date, StreakQueries.IstanbulOffset).ToUniversalTime();
        var capped = EngagementReminderDispatcher.ReminderTypes;

        return (await _dbContext.Notifications.AsNoTracking()
                .Where(notification =>
                    notification.CreatedAt >= todayStart && capped.Contains(notification.NotificationType))
                .Select(notification => notification.RecipientUserId)
                .ToListAsync(cancellationToken))
            .ToHashSet();
    }
}
