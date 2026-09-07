using System.Globalization;
using FluentAssertions;
using Sportner.Application.Features.Identity.UserProfiles;
using Sportner.Localization.Resources;

namespace Sportner.Application.UnitTests.Localization;

public class ProfileErrorsTests
{
    [Theory]
    [InlineData("en-US", "The profile was not found.")]
    [InlineData("tr-TR", "Profil bulunamadı.")]
    public void NotFoundResource_IsLocalized(string cultureName, string expected)
    {
        var value = ErrorMessagesResource.ResourceManager.GetString(
            "Profile_NotFound",
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
            ProfileErrors.NotFound.Message.Should().Be("Profil bulunamadı.");

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            ProfileErrors.NotFound.Message.Should().Be("The profile was not found.");
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
            var trCode = ProfileErrors.NotFound.Code;

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var enCode = ProfileErrors.NotFound.Code;

            trCode.Should().Be("Profile.NotFound");
            enCode.Should().Be("Profile.NotFound");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}
