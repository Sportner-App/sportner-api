using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sportner.Application.Abstractions.BackgroundJobs;
using Sportner.Application.Abstractions.Persistence;

namespace Sportner.Application.BackgroundJobs;

internal sealed class ExpiredSessionCleaner : IExpiredSessionCleaner
{
    private readonly IApplicationDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly BackgroundJobsOptions _options;
    private readonly ILogger<ExpiredSessionCleaner> _logger;

    public ExpiredSessionCleaner(
        IApplicationDbContext dbContext,
        TimeProvider timeProvider,
        IOptions<BackgroundJobsOptions> options,
        ILogger<ExpiredSessionCleaner> logger)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<int> CleanupAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = _timeProvider.GetUtcNow();
        var cutoff = utcNow.AddDays(-Math.Max(1, _options.SessionRetentionDays));
        var batchSize = Math.Clamp(_options.SessionCleanupBatchSize, 1, 5_000);
        var totalDeleted = 0;

        while (true)
        {
            var batch = await _dbContext.UserSessions
                .Where(session =>
                    (session.RevokedAt != null && session.RevokedAt <= cutoff)
                    || session.ExpiresAt <= cutoff)
                .OrderBy(session => session.CreatedAt)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            if (batch.Count == 0)
            {
                break;
            }

            _dbContext.UserSessions.RemoveRange(batch);
            await _dbContext.SaveChangesAsync(cancellationToken);
            totalDeleted += batch.Count;

            if (batch.Count < batchSize)
            {
                break;
            }
        }

        _logger.LogInformation(
            "Expired session cleanup removed {DeletedCount} sessions (cutoff {Cutoff:o}).",
            totalDeleted,
            cutoff);

        return totalDeleted;
    }
}
