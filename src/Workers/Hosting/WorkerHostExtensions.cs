using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.BackgroundJobs;

namespace Sportner.Workers.Hosting;

public static class WorkerHostExtensions
{
    /// <summary>
    /// Registers the services every deployable worker host needs on top of Application/Infrastructure.
    /// </summary>
    public static IServiceCollection AddWorkerHostDefaults(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBackgroundJobsOptions(configuration);
        services.TryAddSingleton<ICurrentUser, SystemCurrentUser>();

        return services;
    }

    public static IServiceCollection AddBackgroundJobsOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<BackgroundJobsOptions>(
            configuration.GetSection(BackgroundJobsOptions.SectionName));

        return services;
    }

    public static IServiceCollection AddCronJob(
        this IServiceCollection services,
        string jobName,
        Func<BackgroundJobsOptions, string> cronSelector,
        Func<IServiceProvider, CancellationToken, Task> execute)
    {
        // Each cron job is its own CronJobHostedService instance, so the registration must be
        // additive; TryAddEnumerable cannot tell factory-built instances apart.
        services.AddSingleton<IHostedService>(provider =>
            new CronJobHostedService(
                jobName,
                cronSelector,
                execute,
                provider.GetRequiredService<IServiceScopeFactory>(),
                provider.GetRequiredService<IOptionsMonitor<BackgroundJobsOptions>>(),
                provider.GetRequiredService<ILogger<CronJobHostedService>>()));

        return services;
    }
}
