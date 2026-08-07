namespace Sportner.Infrastructure.Persistence.Seed;

/// <summary>
/// Populates realistic demo data (users, events, chat, social graph) for local development.
/// Never intended for production: the API only invokes it in the Development environment.
/// </summary>
public interface IDemoDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
