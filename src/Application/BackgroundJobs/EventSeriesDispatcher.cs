using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sportner.Application.Abstractions.BackgroundJobs;
using Sportner.Application.Abstractions.Persistence;

namespace Sportner.Application.BackgroundJobs;

/// <summary>
/// Tekrarlayan etkinlik serilerini ilerletir: bir serinin en son halkası
/// planlanan bitişini geçtiyse ve kota dolmadıysa sıradaki etkinliği açar.
/// Böylece kullanıcı "haftada bir, 3 kez" seçtiğinde 3 etkinlik aynı anda
/// değil, biri bittikçe diğeri oluşur.
/// </summary>
internal sealed class EventSeriesDispatcher : IEventSeriesDispatcher
{
    private readonly IApplicationDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly BackgroundJobsOptions _options;
    private readonly ILogger<EventSeriesDispatcher> _logger;

    public EventSeriesDispatcher(
        IApplicationDbContext dbContext,
        TimeProvider timeProvider,
        IOptions<BackgroundJobsOptions> options,
        ILogger<EventSeriesDispatcher> logger)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<int> DispatchAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = _timeProvider.GetUtcNow();
        var batchSize = Math.Clamp(_options.EventSeriesBatchSize, 1, 200);

        // Adayları seri kimliği düzeyinde topluyoruz: "son halka" kararını her
        // seri için ayrı veriyoruz, yoksa kırpılmış bir sayfa yanlış halkayı
        // son sanıp seriyi çatallayabilir.
        var seriesIds = await _dbContext.Events
            .AsNoTracking()
            .Where(@event =>
                @event.SeriesId != null
                && @event.SeriesSequence < @event.SeriesTotalOccurrences
                && @event.EventDate <= utcNow)
            .Select(@event => @event.SeriesId!.Value)
            .Distinct()
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        var created = 0;

        foreach (var seriesId in seriesIds)
        {
            try
            {
                if (await CreateNextIfDueAsync(seriesId, cancellationToken) is not null)
                {
                    created++;
                }
            }
            catch (Exception exception)
            {
                // Bozuk tek bir seri tüm partiyi durdurmasın.
                _logger.LogError(
                    exception,
                    "Failed to advance recurring event series {SeriesId}.",
                    seriesId);
            }
        }

        _logger.LogInformation(
            "Event series dispatcher created {CreatedCount} follow-up events.",
            created);

        return created;
    }

    public async Task<Guid?> CreateNextIfDueAsync(
        Guid seriesId,
        CancellationToken cancellationToken = default)
    {
        var latest = await _dbContext.Events
            .Where(@event => @event.SeriesId == seriesId)
            .OrderByDescending(@event => @event.SeriesSequence)
            .FirstOrDefaultAsync(cancellationToken);

        if (latest is null || !latest.HasRemainingSeriesOccurrences)
        {
            return null;
        }

        var utcNow = _timeProvider.GetUtcNow();

        // Halka iptal edilmiş olabilir; seri yine de devam eder. Tek koşul
        // planlanan bitişin geçmiş olması.
        if (utcNow < latest.EventDate.AddMinutes(latest.DurationMinutes))
        {
            return null;
        }

        var next = latest.CreateNextSeriesOccurrence(utcNow);
        _dbContext.Events.Add(next);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created occurrence {Sequence}/{Total} ({EventId}) for series {SeriesId}.",
            next.SeriesSequence,
            next.SeriesTotalOccurrences,
            next.Id,
            seriesId);

        return next.Id;
    }
}
