using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Sportner.Infrastructure.Persistence;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(ResolveApiProjectPath())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("SupabaseConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'SupabaseConnection' is not configured.");

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }

    private static string ResolveApiProjectPath()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (current is not null)
        {
            var candidates = new[]
            {
                Path.Combine(current.FullName, "src", "API"),
                Path.Combine(current.FullName, "API")
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(Path.Combine(candidate, "appsettings.json")))
                {
                    return candidate;
                }
            }

            current = current.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate the API project containing appsettings.json.");
    }
}
