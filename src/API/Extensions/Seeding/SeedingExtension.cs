using Microsoft.EntityFrameworkCore;
using Sportner.Infrastructure.Persistence;
using Sportner.Infrastructure.Persistence.Seed;

namespace Sportner.API.Extensions.Seeding;

public static class SeedingExtension
{
    /// <summary>
    /// Applies pending EF Core migrations at API startup (same pattern as Atmaca TMS).
    /// Disable with <c>Database:ApplyMigrationsOnStartup=false</c> if migrations are applied out of band.
    /// Only the API host should run this — workers must not migrate concurrently.
    /// </summary>
    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", true))
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Sportner.DatabaseMigration");

        logger.LogInformation("Applying pending EF Core migrations...");
        await db.Database.MigrateAsync();
        logger.LogInformation("Database migrations are up to date.");
    }

    /// <summary>
    /// Runs idempotent reference-data seeding at startup. Schema must already exist
    /// (see <see cref="MigrateDatabaseAsync"/>).
    /// </summary>
    public static async Task SeedDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>();
        await seeder.SeedAsync();
    }

    /// <summary>
    /// Seeds development-only demo data. Runs only in the Development environment and can be
    /// disabled with <c>Seed:EnableDemoData=false</c>.
    /// </summary>
    public static async Task SeedDemoDataAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        if (!app.Configuration.GetValue("Seed:EnableDemoData", true))
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IDemoDataSeeder>();
        await seeder.SeedAsync();
    }
}
