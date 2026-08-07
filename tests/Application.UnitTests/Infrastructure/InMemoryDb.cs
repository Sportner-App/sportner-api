using Microsoft.EntityFrameworkCore;
using Sportner.Infrastructure.Persistence;

namespace Sportner.Application.UnitTests.Infrastructure;

internal static class InMemoryDb
{
    internal static AppDbContext Create(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString("N"))
            .Options;

        return new AppDbContext(options);
    }
}
