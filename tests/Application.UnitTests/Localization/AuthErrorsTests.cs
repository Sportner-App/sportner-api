using System.Globalization;
using FluentAssertions;
using Sportner.Application.Features.Identity.Auth;
using Sportner.Localization.Resources;

namespace Sportner.Application.UnitTests.Localization;

public class AuthErrorsTests
{
    [Theory]
    [InlineData("en-US", "Username or password is incorrect.")]
    [InlineData("tr-TR", "Kullanıcı adı veya şifre hatalı.")]
    public void InvalidCredentialsResource_IsLocalized(string cultureName, string expected)
    {
        var value = ErrorMessagesResource.ResourceManager.GetString(
            "Auth_InvalidCredentials",
            CultureInfo.GetCultureInfo(cultureName));

        value.Should().Be(expected);
    }

    [Fact]
    public void InvalidCredentials_ReflectsCurrentUICultureOnEachAccess()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
            AuthErrors.InvalidCredentials.Message.Should().Be("Kullanıcı adı veya şifre hatalı.");

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            AuthErrors.InvalidCredentials.Message.Should().Be("Username or password is incorrect.");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public void InvalidCredentials_CodeStaysStableAcrossCultures()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
            var trCode = AuthErrors.InvalidCredentials.Code;

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var enCode = AuthErrors.InvalidCredentials.Code;

            trCode.Should().Be("Auth.InvalidCredentials");
            enCode.Should().Be("Auth.InvalidCredentials");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}
