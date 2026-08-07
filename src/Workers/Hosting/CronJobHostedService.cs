using Cronos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sportner.Application.BackgroundJobs;

namespace Sportner.Workers.Hosting;

/// <summary>
/// Runs a scoped job on a cron schedule (UTC). Shared by deployable worker hosts.
/// </summary>
public sealed class CronJobHostedService : BackgroundService
{
    private readonly string _jobName;
    private readonly Func<BackgroundJobsOptions, string> _cronSelector;
    private readonly Func<IServiceProvider, CancellationToken, Task> _execute;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<BackgroundJobsOptions> _options;
    private readonly ILogger<CronJobHostedService> _logger;

    public CronJobHostedService(
        string jobName,
        Func<BackgroundJobsOptions, string> cronSelector,
        Func<IServiceProvider, CancellationToken, Task> execute,
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<BackgroundJobsOptions> options,
        ILogger<CronJobHostedService> logger)
    {
        _jobName = jobName;
        _cronSelector = cronSelector;
        _execute = execute;
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _options.CurrentValue;

        if (!options.Enabled)
        {
            _logger.LogInformation("Background job {JobName} is disabled.", _jobName);
            return;
        }

        if (options.RunOnStartup)
        {
            await RunOnceAsync(stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            options = _options.CurrentValue;

            if (!options.Enabled)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                continue;
            }

            CronExpression cron;

            try
            {
                cron = CronExpression.Parse(_cronSelector(options));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Invalid cron for {JobName}; retrying in 1 minute.", _jobName);
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                continue;
            }

            var now = DateTimeOffset.UtcNow;
            var next = cron.GetNextOccurrence(now.UtcDateTime, TimeZoneInfo.Utc);

            if (next is null)
            {
                _logger.LogWarning("No next occurrence for {JobName}; stopping scheduler.", _jobName);
                return;
            }

            var delay = next.Value - now.UtcDateTime;

            if (delay > TimeSpan.Zero)
            {
                _logger.LogDebug(
                    "Background job {JobName} next run at {NextRun:o} (delay {Delay}).",
                    _jobName,
                    new DateTimeOffset(next.Value, TimeSpan.Zero),
                    delay);

                await Task.Delay(delay, stoppingToken);
            }

            await RunOnceAsync(stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            await _execute(scope.ServiceProvider, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Background job {JobName} failed.", _jobName);
        }
    }
}
