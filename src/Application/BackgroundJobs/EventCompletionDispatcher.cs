using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sportner.Application.Abstractions.BackgroundJobs;
using Sportner.Application.Abstractions.Gamification;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Features.Events;
using Sportner.Application.Features.Quests;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.BackgroundJobs;

internal sealed class EventCompletionDispatcher : IEventCompletionDispatcher
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IBadgeAwarder _badgeAwarder;
    private readonly IQuestProgressTracker _questProgressTracker;
    private readonly TimeProvider _timeProvider;
    private readonly BackgroundJobsOptions _options;
    private readonly ILogger<EventCompletionDispatcher> _logger;

    public EventCompletionDispatcher(
        IApplicationDbContext dbContext,
        IBadgeAwarder badgeAwarder,
        IQuestProgressTracker questProgressTracker,
        TimeProvider timeProvider,
        IOptions<BackgroundJobsOptions> options,
        ILogger<EventCompletionDispatcher> logger)
    {
        _dbContext = dbContext;
        _badgeAwarder = badgeAwarder;
        _questProgressTracker = questProgressTracker;
        _timeProvider = timeProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<int> DispatchAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = _timeProvider.GetUtcNow();
        var batchSize = Math.Clamp(_options.EventCompletionBatchSize, 1, 500);

        var candidates = await _dbContext.Events
            .AsNoTracking()
            .Where(@event =>
                (@event.Status == EventStatus.Published || @event.Status == EventStatus.Full)
                && @event.EventDate <= utcNow)
            .Select(@event => new { @event.Id, @event.EventDate, @event.DurationMinutes })
            .Take(batchSize * 4)
            .ToListAsync(cancellationToken);

        var dueIds = candidates
            .Where(@event => utcNow >= @event.EventDate.AddMinutes(@event.DurationMinutes))
            .Select(@event => @event.Id)
            .Take(batchSize)
            .ToList();

        var completed = 0;

        foreach (var eventId in dueIds)
        {
            if (await CompleteIfDueAsync(eventId, cancellationToken))
            {
                completed++;
            }
        }

        _logger.LogInformation("Event completion dispatcher closed {CompletedCount} events.", completed);
        return completed;
    }

    public async Task<bool> CompleteIfDueAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        var @event = await EventAccess.LoadAggregateAsync(_dbContext, eventId, cancellationToken);

        if (@event is null)
        {
            return false;
        }

        var utcNow = _timeProvider.GetUtcNow();

        if (!@event.CompleteIfDue(utcNow))
        {
            return false;
        }

        await EventCompletion.ApplySideEffectsAsync(
            _dbContext,
            @event,
            _badgeAwarder,
            _questProgressTracker,
            utcNow,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
