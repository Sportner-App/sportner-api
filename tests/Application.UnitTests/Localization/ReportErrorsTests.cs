using System.Globalization;
using FluentAssertions;
using Sportner.Application.Features.Moderation;
using Sportner.Localization.Resources;

namespace Sportner.Application.UnitTests.Localization;

public class ReportErrorsTests
{
    [Theory]
    [InlineData("en-US", "The report was not found.")]
    [InlineData("tr-TR", "Şikayet bulunamadı.")]
    public void NotFoundResource_IsLocalized(string cultureName, string expected)
    {
        var value = ErrorMessagesResource.ResourceManager.GetString(
            "Report_NotFound",
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
            ReportErrors.NotFound.Message.Should().Be("Şikayet bulunamadı.");

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            ReportErrors.NotFound.Message.Should().Be("The report was not found.");
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
            var trCode = ReportErrors.NotFound.Code;

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var enCode = ReportErrors.NotFound.Code;

            trCode.Should().Be("Report.NotFound");
            enCode.Should().Be("Report.NotFound");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}
