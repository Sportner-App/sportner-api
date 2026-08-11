using Serilog;
using Sportner.Application;
using Sportner.Application.Abstractions.BackgroundJobs;
using Sportner.Application.Abstractions.Gamification;
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
        "event-reminder",
        options => options.EventReminderCron,
        async (provider, ct) =>
        {
            await provider.GetRequiredService<IEventReminderDispatcher>().DispatchAsync(ct);
        });

    builder.Services.AddCronJob(
        "marathon-runner-badge",
        options => options.MarathonRunnerBadgeCron,
        async (provider, ct) =>
        {
            await provider.GetRequiredService<IBadgeAwarder>().SweepMarathonRunnersAsync(ct);
        });

    var host = builder.Build();

    Log.Information("Sportner.Events.Worker starting (event reminders + marathon badge sweep).");
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Sportner.Events.Worker terminated unexpectedly.");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
