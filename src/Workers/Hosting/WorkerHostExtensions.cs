using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sportner.Application.BackgroundJobs;

namespace Sportner.Workers.Hosting;

public static class WorkerHostExtensions
{
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
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IHostedService>(provider =>
                new CronJobHostedService(
                    jobName,
                    cronSelector,
                    execute,
                    provider.GetRequiredService<IServiceScopeFactory>(),
                    provider.GetRequiredService<IOptionsMonitor<BackgroundJobsOptions>>(),
                    provider.GetRequiredService<ILogger<CronJobHostedService>>())));

        return services;
    }
}
