using System.Globalization;
using FluentAssertions;
using Sportner.Application.Features.Identity.Sessions;
using Sportner.Localization.Resources;

namespace Sportner.Application.UnitTests.Localization;

public class SessionErrorsTests
{
    [Theory]
    [InlineData("en-US", "The session was not found.")]
    [InlineData("tr-TR", "Oturum bulunamadı.")]
    public void NotFoundResource_IsLocalized(string cultureName, string expected)
    {
        var value = ErrorMessagesResource.ResourceManager.GetString(
            "Session_NotFound",
            CultureInfo.GetCultureInfo(cultureName));

        value.Should().Be(expected);
    }

    [Fact]
    public void NotFound_ReflectsCurrentUICultureOnEachAccess()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
            SessionErrors.NotFound.Message.Should().Be("Oturum bulunamadı.");

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            SessionErrors.NotFound.Message.Should().Be("The session was not found.");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public void NotFound_CodeStaysStableAcrossCultures()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
            var trCode = SessionErrors.NotFound.Code;

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var enCode = SessionErrors.NotFound.Code;

            trCode.Should().Be("Session.NotFound");
            enCode.Should().Be("Session.NotFound");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}
