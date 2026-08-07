using Microsoft.Extensions.Logging;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.BackgroundJobs;

namespace Sportner.Infrastructure.Authentication;

public sealed class OtpCleaner : IOtpCleaner
{
    private readonly IOtpChallengeStore _store;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<OtpCleaner> _logger;

    public OtpCleaner(
        IOtpChallengeStore store,
        TimeProvider timeProvider,
        ILogger<OtpCleaner> logger)
    {
        _store = store;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<int> CleanupAsync(CancellationToken cancellationToken = default)
    {
        var removed = await _store.RemoveExpiredAsync(_timeProvider.GetUtcNow(), cancellationToken);

        _logger.LogInformation("OTP cleanup removed {RemovedCount} expired challenges.", removed);
        return removed;
    }
}
