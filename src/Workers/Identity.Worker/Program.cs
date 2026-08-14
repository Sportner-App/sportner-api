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

    builder.Services.AddApplication(builder.Configuration);
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddWorkerHostDefaults(builder.Configuration);

    builder.Services.AddCronJob(
        "session-cleanup",
        options => options.SessionCleanupCron,
        async (provider, ct) =>
        {
            await provider.GetRequiredService<IExpiredSessionCleaner>().CleanupAsync(ct);
        });

    var host = builder.Build();

    Log.Information("Sportner.Identity.Worker starting (session cleanup).");
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Sportner.Identity.Worker terminated unexpectedly.");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
