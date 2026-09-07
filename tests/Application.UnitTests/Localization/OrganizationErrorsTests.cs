using System.Globalization;
using FluentAssertions;
using Sportner.Application.Features.Organizations;
using Sportner.Localization.Resources;

namespace Sportner.Application.UnitTests.Localization;

public class OrganizationErrorsTests
{
    [Theory]
    [InlineData("en-US", "The organization was not found.")]
    [InlineData("tr-TR", "Organizasyon bulunamadı.")]
    public void NotFoundResource_IsLocalized(string cultureName, string expected)
    {
        var value = ErrorMessagesResource.ResourceManager.GetString(
            "Organization_NotFound",
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
            OrganizationErrors.NotFound.Message.Should().Be("Organizasyon bulunamadı.");

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            OrganizationErrors.NotFound.Message.Should().Be("The organization was not found.");
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
            var trCode = OrganizationErrors.NotFound.Code;

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var enCode = OrganizationErrors.NotFound.Code;

            trCode.Should().Be("Organization.NotFound");
            enCode.Should().Be("Organization.NotFound");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}
