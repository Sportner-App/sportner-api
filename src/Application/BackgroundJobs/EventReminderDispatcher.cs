using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sportner.Application.Abstractions.BackgroundJobs;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Events;

namespace Sportner.Application.BackgroundJobs;

internal sealed class EventReminderDispatcher : IEventReminderDispatcher
{
    private readonly IApplicationDbContext _dbContext;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly TimeProvider _timeProvider;
    private readonly BackgroundJobsOptions _options;
    private readonly ILogger<EventReminderDispatcher> _logger;

    public EventReminderDispatcher(
        IApplicationDbContext dbContext,
        INotificationPublisher notificationPublisher,
        TimeProvider timeProvider,
        IOptions<BackgroundJobsOptions> options,
        ILogger<EventReminderDispatcher> logger)
    {
        _dbContext = dbContext;
        _notificationPublisher = notificationPublisher;
        _timeProvider = timeProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<int> DispatchAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = _timeProvider.GetUtcNow();
        var windows = (_options.EventReminderWindowsMinutes is { Length: > 0 }
                ? _options.EventReminderWindowsMinutes
                : [1440, 60])
            .Where(minutes => minutes > 0)
            .Distinct()
            .OrderByDescending(minutes => minutes)
            .ToArray();

        if (windows.Length == 0)
        {
            return 0;
        }

        // Catch the window shortly after it opens (cron is every ~15 minutes).
        var grace = TimeSpan.FromMinutes(20);
        var maxWindow = windows[0];
        var horizonStart = utcNow;
        var horizonEnd = utcNow.AddMinutes(maxWindow);

        var events = await _dbContext.Events
            .AsNoTracking()
            .Where(@event =>
                (@event.Status == EventStatus.Published || @event.Status == EventStatus.Full)
                && @event.EventDate > horizonStart
                && @event.EventDate <= horizonEnd)
            .Select(@event => new { @event.Id, @event.Title, @event.EventDate, @event.OrganizerUserId })
            .ToListAsync(cancellationToken);

        var sent = 0;

        foreach (var @event in events)
        {
            foreach (var windowMinutes in windows)
            {
                var threshold = @event.EventDate.AddMinutes(-windowMinutes);

                if (utcNow < threshold
                    || utcNow >= threshold + grace
                    || utcNow >= @event.EventDate)
                {
                    continue;
                }

                var participantUserIds = await _dbContext.EventParticipants
                    .AsNoTracking()
                    .Where(participant =>
                        participant.EventId == @event.Id
                        && participant.UserId != null
                        && participant.UserId != @event.OrganizerUserId
                        && participant.Status == ParticipantStatus.Approved)
                    .Select(participant => participant.UserId!.Value)
                    .ToListAsync(cancellationToken);

                foreach (var userId in participantUserIds)
                {
                    var alreadySent = await _dbContext.EventReminderDispatches
                        .AsNoTracking()
                        .AnyAsync(
                            dispatch =>
                                dispatch.EventId == @event.Id
                                && dispatch.UserId == userId
                                && dispatch.WindowMinutes == windowMinutes,
                            cancellationToken);

                    if (alreadySent)
                    {
                        continue;
                    }

                    var label = windowMinutes >= 1440
                        ? $"{windowMinutes / 1440} gün"
                        : windowMinutes >= 60
                            ? $"{windowMinutes / 60} saat"
                            : $"{windowMinutes} dk";

                    await _notificationPublisher.PublishAsync(
                        userId,
                        NotificationType.EventReminder,
                        "Etkinlik hatırlatması",
                        $"\"{@event.Title}\" etkinliği {label} içinde başlıyor.",
                        NotificationEntityType.Event,
                        @event.Id,
                        actorUserId: null,
                        cancellationToken);

                    _dbContext.EventReminderDispatches.Add(
                        EventReminderDispatch.Create(@event.Id, userId, windowMinutes, utcNow));

                    sent++;
                }
            }
        }

        if (sent > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Event reminder dispatcher sent {SentCount} reminders.", sent);
        return sent;
    }
}
