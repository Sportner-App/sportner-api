using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sportner.Application.Abstractions.BackgroundJobs;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Features.Identity.UserProfiles.Streaks;
using Sportner.Application.Features.Notifications;
using Sportner.Domain.Common.Enums;
using Sportner.Localization.Resources;

namespace Sportner.Application.BackgroundJobs;

/// <summary>
/// Günde bir kez (18:00 İstanbul) çalışan hatırlatıcılar. Her kullanıcıya en fazla
/// bir tane gider; öncelik sırası: seri tehlikede (perşembe) → haftalık hedef
/// (cumartesi) → sevdiği sporlardan yeni etkinlik → geri dönüş. Söyleyecek gerçek
/// bir şey yoksa hiçbir şey gönderilmez. Sessiz saatler cron saatiyle garanti.
/// </summary>
internal sealed class EngagementReminderDispatcher : IEngagementReminderDispatcher
{
    internal static readonly NotificationType[] ReminderTypes =
    [
        NotificationType.StreakAtRisk,
        NotificationType.WeeklyGoalNudge,
        NotificationType.NewEventsNearby,
        NotificationType.ComeBack
    ];

    private static readonly TimeSpan InactiveThreshold = TimeSpan.FromDays(14);
    private static readonly TimeSpan ComeBackCooldown = TimeSpan.FromDays(7);
    private static readonly TimeSpan NewEventWindow = TimeSpan.FromHours(24);

    private readonly IApplicationDbContext _dbContext;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<EngagementReminderDispatcher> _logger;

    public EngagementReminderDispatcher(
        IApplicationDbContext dbContext,
        INotificationPublisher notificationPublisher,
        TimeProvider timeProvider,
        ILogger<EngagementReminderDispatcher> logger)
    {
        _dbContext = dbContext;
        _notificationPublisher = notificationPublisher;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<int> DispatchAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = _timeProvider.GetUtcNow();
        var localNow = utcNow.ToOffset(StreakQueries.IstanbulOffset);
        var todayStart = new DateTimeOffset(localNow.Date, StreakQueries.IstanbulOffset);
        var weekEnd = new DateTimeOffset(
            StreakQueries.WeekStartOf(utcNow).AddDays(7).ToDateTime(TimeOnly.MinValue),
            StreakQueries.IstanbulOffset);

        var users = await (
                from user in _dbContext.Users.AsNoTracking()
                join profile in _dbContext.UserProfiles.AsNoTracking()
                    on user.Id equals profile.UserId
                where user.Status == UserStatus.Active
                select new { user.Id, user.LastSeenAt, profile.City, profile.WeeklyGoal })
            .ToListAsync(cancellationToken);

        // Bugün zaten bir hatırlatıcı ya da etkinlik hatırlatması alan atlanır.
        var notifiedToday = (await _dbContext.Notifications.AsNoTracking()
                .Where(notification =>
                    notification.CreatedAt >= todayStart
                    && (ReminderTypes.Contains(notification.NotificationType)
                        || notification.NotificationType == NotificationType.EventReminder))
                .Select(notification => notification.RecipientUserId)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var comeBackCooldownStart = utcNow - ComeBackCooldown;
        var recentComeBack = (await _dbContext.Notifications.AsNoTracking()
                .Where(notification =>
                    notification.NotificationType == NotificationType.ComeBack
                    && notification.CreatedAt >= comeBackCooldownStart)
                .Select(notification => notification.RecipientUserId)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var streakSince = utcNow.AddDays(-400);
        var attended = await (
                from participant in _dbContext.EventParticipants.AsNoTracking()
                join @event in _dbContext.Events.AsNoTracking()
                    on participant.EventId equals @event.Id
                where participant.UserId != null
                    && participant.Status == ParticipantStatus.Attended
                    && @event.Status != EventStatus.Cancelled
                    && @event.EventDate >= streakSince
                select new { UserId = participant.UserId!.Value, @event.Id, @event.EventDate })
            .ToListAsync(cancellationToken);
        var organized = await _dbContext.Events.AsNoTracking()
            .Where(@event => @event.Status == EventStatus.Completed && @event.EventDate >= streakSince)
            .Select(@event => new { UserId = @event.OrganizerUserId, @event.Id, @event.EventDate })
            .ToListAsync(cancellationToken);
        var playedByUser = attended
            .Concat(organized)
            .GroupBy(item => item.UserId)
            .ToDictionary(
                group => group.Key,
                group => group.DistinctBy(item => item.Id).Select(item => item.EventDate).ToList());

        var upcoming = await _dbContext.Events.AsNoTracking()
            .Where(@event =>
                (@event.Status == EventStatus.Published || @event.Status == EventStatus.Full)
                && @event.EventDate > utcNow
                && @event.EventDate < weekEnd.AddDays(7))
            .Select(@event => new UpcomingEvent(
                @event.Id,
                @event.Title,
                @event.SportId,
                @event.Address,
                @event.OrganizerUserId,
                @event.CreatedAt,
                @event.EventDate,
                @event.Status == EventStatus.Published))
            .ToListAsync(cancellationToken);
        var upcomingIds = upcoming.Select(@event => @event.Id).ToList();

        var joinedEventIds = (await _dbContext.EventParticipants.AsNoTracking()
                .Where(participant =>
                    upcomingIds.Contains(participant.EventId) && participant.UserId != null)
                .Select(participant => new { UserId = participant.UserId!.Value, participant.EventId })
                .ToListAsync(cancellationToken))
            .ToLookup(item => item.UserId, item => item.EventId);

        var sportsByUser = (await _dbContext.UserSports.AsNoTracking()
                .Select(userSport => new { userSport.UserId, userSport.SportId })
                .ToListAsync(cancellationToken))
            .ToLookup(item => item.UserId, item => item.SportId);

        var candidates = users.Where(user => !notifiedToday.Contains(user.Id)).ToList();
        var languages = await NotificationActor.ResolveRecipientLanguagesAsync(
            _dbContext, candidates.Select(user => user.Id).ToList(), cancellationToken);

        var sent = 0;
        foreach (var user in candidates)
        {
            var cityEvents = string.IsNullOrWhiteSpace(user.City)
                ? []
                : upcoming.Where(@event => @event.Address.Contains(user.City)).ToList();
            var joined = joinedEventIds[user.Id].ToHashSet();
            var userSports = sportsByUser[user.Id].ToHashSet();

            var reminder = Decide(new ReminderContext(
                utcNow,
                localNow.DayOfWeek,
                StreakQueries.Calculate(
                    playedByUser.GetValueOrDefault(user.Id) ?? [],
                    user.WeeklyGoal,
                    utcNow),
                cityEvents.Count(@event => @event.EventDate < weekEnd && @event.IsOpen),
                cityEvents
                    .Where(@event =>
                        @event.IsOpen
                        && @event.CreatedAt >= utcNow - NewEventWindow
                        && @event.OrganizerUserId != user.Id
                        && userSports.Contains(@event.SportId)
                        && !joined.Contains(@event.Id))
                    .ToList(),
                user.LastSeenAt,
                recentComeBack.Contains(user.Id)));

            if (reminder is null)
            {
                continue;
            }

            var language = languages.GetValueOrDefault(user.Id);
            var (title, body) = Render(reminder, user.City, language);

            await _notificationPublisher.PublishAsync(
                user.Id,
                reminder.Type,
                title,
                body,
                reminder.EventId is null ? NotificationEntityType.User : NotificationEntityType.Event,
                reminder.EventId,
                actorUserId: null,
                cancellationToken);
            sent++;
        }

        _logger.LogInformation("Engagement reminder dispatcher sent {SentCount} reminders.", sent);
        return sent;
    }

    internal static Reminder? Decide(ReminderContext context)
    {
        var streak = context.Streak;

        if (context.DayOfWeek == DayOfWeek.Thursday
            && streak.CurrentStreak > 0
            && streak.ThisWeekCount == 0)
        {
            return new Reminder(
                NotificationType.StreakAtRisk, streak.CurrentStreak, context.CityEventsThisWeek, null, null);
        }

        if (context.DayOfWeek == DayOfWeek.Saturday
            && streak.ThisWeekCount > 0
            && streak.ThisWeekCount < streak.WeeklyGoal)
        {
            return new Reminder(
                NotificationType.WeeklyGoalNudge, streak.WeeklyGoal - streak.ThisWeekCount, 0, null, null);
        }

        if (context.NewEvents.Count > 0)
        {
            var single = context.NewEvents.Count == 1 ? context.NewEvents[0] : null;
            return new Reminder(
                NotificationType.NewEventsNearby, context.NewEvents.Count, 0, single?.Id, single?.Title);
        }

        if (context.LastSeenAt is { } lastSeen
            && context.UtcNow - lastSeen >= InactiveThreshold
            && !context.ComeBackSentRecently
            && context.CityEventsThisWeek > 0)
        {
            return new Reminder(NotificationType.ComeBack, context.CityEventsThisWeek, 0, null, null);
        }

        return null;
    }

    private static (string Title, string Body) Render(Reminder reminder, string? city, Language language)
    {
        string F(string key, params object[] args) => NotificationActor.Format(language, key, args);

        return reminder.Type switch
        {
            NotificationType.StreakAtRisk => (
                F(nameof(NotificationsResource.StreakAtRisk_Title), reminder.Count),
                reminder.SecondaryCount > 0 && city is not null
                    ? F(nameof(NotificationsResource.StreakAtRisk_BodyWithEvents), city, reminder.SecondaryCount)
                    : F(nameof(NotificationsResource.StreakAtRisk_Body))),
            NotificationType.WeeklyGoalNudge => (
                F(nameof(NotificationsResource.WeeklyGoalNudge_Title), reminder.Count),
                F(nameof(NotificationsResource.WeeklyGoalNudge_Body))),
            NotificationType.NewEventsNearby => (
                F(nameof(NotificationsResource.NewEventsNearby_Title), city ?? string.Empty),
                reminder.EventTitle is { } title
                    ? F(nameof(NotificationsResource.NewEventsNearby_BodySingle), title)
                    : F(nameof(NotificationsResource.NewEventsNearby_Body), reminder.Count)),
            _ => (
                F(nameof(NotificationsResource.ComeBack_Title)),
                F(nameof(NotificationsResource.ComeBack_Body), city ?? string.Empty, reminder.Count))
        };
    }

    internal sealed record UpcomingEvent(
        Guid Id,
        string Title,
        Guid SportId,
        string Address,
        Guid OrganizerUserId,
        DateTimeOffset CreatedAt,
        DateTimeOffset EventDate,
        bool IsOpen);

    internal sealed record ReminderContext(
        DateTimeOffset UtcNow,
        DayOfWeek DayOfWeek,
        StreakResponse Streak,
        int CityEventsThisWeek,
        IReadOnlyList<UpcomingEvent> NewEvents,
        DateTimeOffset? LastSeenAt,
        bool ComeBackSentRecently);

    internal sealed record Reminder(
        NotificationType Type,
        int Count,
        int SecondaryCount,
        Guid? EventId,
        string? EventTitle);
}
