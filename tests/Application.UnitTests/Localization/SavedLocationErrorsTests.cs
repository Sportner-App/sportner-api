using System.Globalization;
using FluentAssertions;
using Sportner.Application.Features.Identity.SavedLocations;
using Sportner.Localization.Resources;

namespace Sportner.Application.UnitTests.Localization;

public class SavedLocationErrorsTests
{
    [Theory]
    [InlineData("en-US", "The saved location was not found.")]
    [InlineData("tr-TR", "Kayıtlı konum bulunamadı.")]
    public void NotFoundResource_IsLocalized(string cultureName, string expected)
    {
        var value = ErrorMessagesResource.ResourceManager.GetString(
            "SavedLocation_NotFound",
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
            SavedLocationErrors.NotFound.Message.Should().Be("Kayıtlı konum bulunamadı.");

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            SavedLocationErrors.NotFound.Message.Should().Be("The saved location was not found.");
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
            var trCode = SavedLocationErrors.NotFound.Code;

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var enCode = SavedLocationErrors.NotFound.Code;

            trCode.Should().Be("SavedLocation.NotFound");
            enCode.Should().Be("SavedLocation.NotFound");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}
