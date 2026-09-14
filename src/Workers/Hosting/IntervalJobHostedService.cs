using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sportner.Application.BackgroundJobs;

namespace Sportner.Workers.Hosting;

/// <summary>
/// Runs a scoped job on a fixed interval (UTC). For polling loops that need sub-minute
/// granularity, where a cron expression's 1-minute resolution is too coarse.
/// </summary>
public sealed class IntervalJobHostedService : BackgroundService
{
    private readonly string _jobName;
    private readonly Func<BackgroundJobsOptions, TimeSpan> _intervalSelector;
    private readonly Func<IServiceProvider, CancellationToken, Task> _execute;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<BackgroundJobsOptions> _options;
    private readonly ILogger<IntervalJobHostedService> _logger;

    public IntervalJobHostedService(
        string jobName,
        Func<BackgroundJobsOptions, TimeSpan> intervalSelector,
        Func<IServiceProvider, CancellationToken, Task> execute,
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<BackgroundJobsOptions> options,
        ILogger<IntervalJobHostedService> logger)
    {
        _jobName = jobName;
        _intervalSelector = intervalSelector;
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

        _logger.LogInformation(
            "Background job {JobName} started with a {IntervalSeconds}-second interval.",
            _jobName,
            _intervalSelector(options).TotalSeconds);

        await RunOnceAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            options = _options.CurrentValue;

            if (!options.Enabled)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                continue;
            }

            await Task.Delay(_intervalSelector(options), stoppingToken);
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
