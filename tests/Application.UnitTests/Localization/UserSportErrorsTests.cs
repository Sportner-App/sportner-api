using System.Globalization;
using FluentAssertions;
using Sportner.Application.Features.Identity.UserSports;
using Sportner.Localization.Resources;

namespace Sportner.Application.UnitTests.Localization;

public class UserSportErrorsTests
{
    [Theory]
    [InlineData("en-US", "This sport is not associated with the user.")]
    [InlineData("tr-TR", "Bu spor profiline ekli değil.")]
    public void NotFoundResource_IsLocalized(string cultureName, string expected)
    {
        var value = ErrorMessagesResource.ResourceManager.GetString(
            "UserSport_NotFound",
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
            UserSportErrors.NotFound.Message.Should().Be("Bu spor profiline ekli değil.");

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            UserSportErrors.NotFound.Message.Should().Be("This sport is not associated with the user.");
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
            var trCode = UserSportErrors.NotFound.Code;

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var enCode = UserSportErrors.NotFound.Code;

            trCode.Should().Be("UserSport.NotFound");
            enCode.Should().Be("UserSport.NotFound");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}
