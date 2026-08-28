using Sportner.Application.Abstractions.BackgroundJobs;

namespace Sportner.API.BackgroundServices;

/// <summary>
/// Drains the push notification outbox from the API process for hosting plans
/// where a dedicated background worker is not available.
/// </summary>
internal sealed class ApiNotificationDeliveryService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiNotificationDeliveryService> _logger;

    public ApiNotificationDeliveryService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<ApiNotificationDeliveryService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_configuration.GetValue("PushDelivery:RunInApi", true))
        {
            _logger.LogInformation("API push delivery is disabled; a dedicated worker must process the outbox.");
            return;
        }

        _logger.LogInformation("API push delivery started with a {IntervalSeconds}-second interval.", Interval.TotalSeconds);

        await DispatchAsync(stoppingToken);

        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await DispatchAsync(stoppingToken);
        }
    }

    private async Task DispatchAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            await scope.ServiceProvider
                .GetRequiredService<INotificationDeliveryDispatcher>()
                .DispatchPendingAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal application shutdown.
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "API push delivery cycle failed.");
        }
    }
}
