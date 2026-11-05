using Sportner.Application.Abstractions.BackgroundJobs;

namespace Sportner.API.BackgroundServices;

/// <summary>
/// Reads push receipts from the API process, alongside <see cref="ApiNotificationDeliveryService"/>
/// and under the same switch.
/// </summary>
internal sealed class ApiPushReceiptService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiPushReceiptService> _logger;

    public ApiPushReceiptService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<ApiPushReceiptService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_configuration.GetValue("PushDelivery:RunInApi", true))
        {
            return;
        }

        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                await scope.ServiceProvider
                    .GetRequiredService<IPushReceiptChecker>()
                    .CheckDueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal application shutdown.
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "API push receipt check failed.");
            }
        }
    }
}
