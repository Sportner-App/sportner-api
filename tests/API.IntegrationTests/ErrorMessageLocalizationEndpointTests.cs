using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Sportner.API.IntegrationTests;

/// <summary>
/// Unit testler CurrentUICulture'ı elle set eder; bu test HTTP pipeline'ının
/// Accept-Language header'ını doğru kültüre çevirip error.message'ı buna göre
/// döndürdüğünü doğrular.
/// </summary>
public class ErrorMessageLocalizationEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ErrorMessageLocalizationEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("tr-TR", "Kullanıcı adı veya şifre hatalı.")]
    [InlineData("en-US", "Username or password is incorrect.")]
    public async Task LoginFailure_MessageFollowsAcceptLanguage(
        string acceptLanguage,
        string expectedDetail)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Accept-Language", acceptLanguage);

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = "missing-user", password = "wrong-password" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("detail").GetString()
            .Should().Be(expectedDetail);
    }

    [Theory]
    [InlineData("tr-TR", "Spor bulunamadı.")]
    [InlineData("en-US", "The sport was not found.")]
    public async Task SportNotFound_MessageFollowsAcceptLanguage(
        string acceptLanguage,
        string expectedDetail)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Accept-Language", acceptLanguage);

        var response = await client.GetAsync("/api/sports/__does-not-exist__");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("detail").GetString()
            .Should().Be(expectedDetail);
    }
}
