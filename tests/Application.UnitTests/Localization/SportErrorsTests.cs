using System.Globalization;
using FluentAssertions;
using Sportner.Application.Features.Catalog.Sports;
using Sportner.Localization.Resources;

namespace Sportner.Application.UnitTests.Localization;

public class SportErrorsTests
{
    [Theory]
    [InlineData("en-US", "The sport was not found.")]
    [InlineData("tr-TR", "Spor bulunamadı.")]
    public void NotFoundResource_IsLocalized(string cultureName, string expected)
    {
        var value = ErrorMessagesResource.ResourceManager.GetString(
            "Sport_NotFound",
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
            SportErrors.NotFound.Message.Should().Be("Spor bulunamadı.");

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            SportErrors.NotFound.Message.Should().Be("The sport was not found.");
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
            var trCode = SportErrors.NotFound.Code;

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var enCode = SportErrors.NotFound.Code;

            trCode.Should().Be("Sport.NotFound");
            enCode.Should().Be("Sport.NotFound");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}
