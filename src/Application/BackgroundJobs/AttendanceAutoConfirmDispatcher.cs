using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sportner.Application.Abstractions.BackgroundJobs;
using Sportner.Application.Abstractions.Gamification;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Features.Events;
using Sportner.Application.Features.Quests;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.BackgroundJobs;

internal sealed class AttendanceAutoConfirmDispatcher : IAttendanceAutoConfirmDispatcher
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IBadgeAwarder _badgeAwarder;
    private readonly IQuestProgressTracker _questProgressTracker;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly TimeProvider _timeProvider;
    private readonly BackgroundJobsOptions _options;
    private readonly ILogger<AttendanceAutoConfirmDispatcher> _logger;

    public AttendanceAutoConfirmDispatcher(
        IApplicationDbContext dbContext,
        IBadgeAwarder badgeAwarder,
        IQuestProgressTracker questProgressTracker,
        INotificationPublisher notificationPublisher,
        TimeProvider timeProvider,
        IOptions<BackgroundJobsOptions> options,
        ILogger<AttendanceAutoConfirmDispatcher> logger)
    {
        _dbContext = dbContext;
        _badgeAwarder = badgeAwarder;
        _questProgressTracker = questProgressTracker;
        _notificationPublisher = notificationPublisher;
        _timeProvider = timeProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<int> DispatchAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = _timeProvider.GetUtcNow();
        var graceDays = Math.Max(_options.AttendanceAutoConfirmGraceDays, 1);
        var batchSize = Math.Clamp(_options.EventCompletionBatchSize, 1, 500);

        var candidates = await _dbContext.Events
            .AsNoTracking()
            .Where(@event =>
                @event.Status == EventStatus.Completed
                && _dbContext.EventParticipants.Any(participant =>
                    participant.EventId == @event.Id
                    && participant.UserId != null
                    && participant.Status == ParticipantStatus.Approved))
            .Select(@event => new { @event.Id, @event.EventDate, @event.DurationMinutes })
            .Take(batchSize * 4)
            .ToListAsync(cancellationToken);

        var dueEventIds = candidates
            .Where(@event => utcNow >= @event.EventDate.AddMinutes(@event.DurationMinutes).AddDays(graceDays))
            .Select(@event => @event.Id)
            .Take(batchSize)
            .ToList();

        var confirmed = 0;

        foreach (var eventId in dueEventIds)
        {
            confirmed += await AutoConfirmEventAsync(eventId, utcNow, cancellationToken);
        }

        _logger.LogInformation(
            "Attendance auto-confirm dispatcher confirmed {ConfirmedCount} participants across {EventCount} events.",
            confirmed,
            dueEventIds.Count);

        return confirmed;
    }

    private async Task<int> AutoConfirmEventAsync(
        Guid eventId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var @event = await EventAccess.LoadAggregateAsync(_dbContext, eventId, cancellationToken);

        if (@event is null)
        {
            return 0;
        }

        var pendingUserIds = @event.Participants
            .Where(participant =>
                participant.UserId is not null
                && participant.Status == ParticipantStatus.Approved)
            .Select(participant => participant.UserId!.Value)
            .ToList();

        foreach (var userId in pendingUserIds)
        {
            await AttendanceConfirmation.ConfirmAsync(
                _dbContext,
                @event,
                userId,
                _badgeAwarder,
                _questProgressTracker,
                _notificationPublisher,
                utcNow,
                cancellationToken);
        }

        if (pendingUserIds.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return pendingUserIds.Count;
    }
}
