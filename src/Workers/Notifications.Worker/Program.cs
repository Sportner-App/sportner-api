using Serilog;
using Sportner.Application;
using Sportner.Application.Abstractions.BackgroundJobs;
using Sportner.Infrastructure;
using Sportner.Workers.Hosting;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog((services, configuration) =>
        configuration
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console());

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddBackgroundJobsOptions(builder.Configuration);

    builder.Services.AddCronJob(
        "notification-delivery",
        options => options.NotificationDeliveryCron,
        async (provider, ct) =>
        {
            await provider.GetRequiredService<INotificationDeliveryDispatcher>()
                .DispatchPendingAsync(ct);
        });

    var host = builder.Build();

    Log.Information("Sportner.Notifications.Worker starting (push delivery outbox).");
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Sportner.Notifications.Worker terminated unexpectedly.");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
