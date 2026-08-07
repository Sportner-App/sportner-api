namespace Sportner.Infrastructure.Persistence.Seed;

/// <summary>
/// Populates reference data (Sports, Badges, ReportReasons) that the product relies on.
/// Idempotent: safe to run on every startup.
/// </summary>
public interface IDatabaseSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
