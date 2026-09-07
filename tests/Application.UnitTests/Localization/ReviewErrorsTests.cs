using System.Globalization;
using FluentAssertions;
using Sportner.Application.Features.Reviews;
using Sportner.Localization.Resources;

namespace Sportner.Application.UnitTests.Localization;

public class ReviewErrorsTests
{
    [Theory]
    [InlineData("en-US", "The review was not found.")]
    [InlineData("tr-TR", "Değerlendirme bulunamadı.")]
    public void NotFoundResource_IsLocalized(string cultureName, string expected)
    {
        var value = ErrorMessagesResource.ResourceManager.GetString(
            "Review_NotFound",
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
            ReviewErrors.NotFound.Message.Should().Be("Değerlendirme bulunamadı.");

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            ReviewErrors.NotFound.Message.Should().Be("The review was not found.");
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
            var trCode = ReviewErrors.NotFound.Code;

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var enCode = ReviewErrors.NotFound.Code;

            trCode.Should().Be("Review.NotFound");
            enCode.Should().Be("Review.NotFound");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}
