using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Sportner.API.IntegrationTests;

public class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("JwtSettings:Secret", "integration-test-secret-key-32chars!!");
            builder.UseSetting("JwtSettings:Issuer", "SportnerApi");
            builder.UseSetting("JwtSettings:Audience", "SportnerMobile");
            builder.UseSetting(
                "ConnectionStrings:SupabaseConnection",
                "Host=127.0.0.1;Port=5432;Database=sportner_test;Username=postgres;Password=postgres");
            builder.UseSetting("Supabase:Url", "https://example.supabase.co");
            builder.UseSetting("Supabase:ServiceRoleKey", "test-key");
        }).CreateClient();
    }

    [Fact]
    public async Task HealthLive_ReturnsOk()
    {
        var response = await _client.GetAsync("/health/live");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
