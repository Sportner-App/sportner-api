using Sportner.Infrastructure.Persistence.Seed;

namespace Sportner.API.Extensions.Seeding;

public static class SeedingExtension
{
    /// <summary>
    /// Runs idempotent reference-data seeding at startup. Assumes the database schema already
    /// exists (migrations are applied out of band).
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
