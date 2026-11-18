using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Sportner.API.IntegrationTests;

public class HealthEndpointTests : IClassFixture<SportnerApiFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(SportnerApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthLive_ReturnsOk()
    {
        var response = await _client.GetAsync("/health/live");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
