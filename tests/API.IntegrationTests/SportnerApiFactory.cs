using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;

namespace Sportner.API.IntegrationTests;

/// <summary>
/// Entegrasyon testleri icin WebApplicationFactory. 17 Kas 2026'da yerel <c>dotnet test</c>,
/// Development ortaminda user-secrets'taki canli Supabase baglantisini kullanip canli DB'ye
/// migration uygulamis ve demo verisi eklemisti. Bu factory baglantiyi yerel bir test
/// veritabanina sabitler, demo seed'i kapatir ve yerel olmayan hostlari reddeder.
/// Baglanti sirasi: SPORTNER_TEST_DB, ConnectionStrings__SupabaseConnection (CI), varsayilan yerel Postgres.
/// Yerelde calistirmak icin:
/// <c>docker run --rm -d --name sportner-test-db -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=sportner_test -p 5432:5432 postgres:16</c>
/// </summary>
public sealed class SportnerApiFactory : WebApplicationFactory<Program>
{
    private const string DefaultConnection =
        "Host=localhost;Port=5432;Database=sportner_test;Username=postgres;Password=postgres;SSL Mode=Disable";

    private static readonly string ResolvedConnection = ResolveAndGuard();

    static SportnerApiFactory()
    {
        // Ortam degiskenleri user-secrets'tan sonra yuklenir; host kurulmadan once ayarla.
        Environment.SetEnvironmentVariable("ConnectionStrings__SupabaseConnection", ResolvedConnection);
        Environment.SetEnvironmentVariable("Seed__EnableDemoData", "false");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:SupabaseConnection", ResolvedConnection);
        builder.UseSetting("Seed:EnableDemoData", "false");
        // Testlerin hepsi tek bir "IP"den (TestServer'da null) gelir; genel tavan onları
        // birbirine karıştırıp 429'a düşürmesin. Uç noktaya özel politikalar açık kalır.
        builder.UseSetting("RateLimiting:GlobalEnabled", "false");
    }

    private static string ResolveAndGuard()
    {
        var connection = FirstNonEmpty(
            Environment.GetEnvironmentVariable("SPORTNER_TEST_DB"),
            Environment.GetEnvironmentVariable("ConnectionStrings__SupabaseConnection"))
            ?? DefaultConnection;

        var host = new NpgsqlConnectionStringBuilder(connection).Host ?? string.Empty;
        var isSupabase = host.Contains("supabase.co", StringComparison.OrdinalIgnoreCase)
            || host.Contains("supabase.com", StringComparison.OrdinalIgnoreCase);
        var isLocal = host is "localhost" or "127.0.0.1" or "::1";
        var allowRemote = Environment.GetEnvironmentVariable("SPORTNER_TEST_DB_ALLOW_REMOTE") == "true";

        if (isSupabase || (!isLocal && !allowRemote))
        {
            throw new InvalidOperationException(
                $"Test veritabani yerel degil (host: '{host}'). Canli veriye yazmamak icin test durduruldu. " +
                "SPORTNER_TEST_DB ortam degiskeniyle yerel bir Postgres baglantisi verin.");
        }

        return connection;
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
}
