using System.Globalization;
using FluentAssertions;
using Sportner.Application.Features.Organizations;
using Sportner.Localization.Resources;

namespace Sportner.Application.UnitTests.Localization;

public class OrganizationErrorsTests
{
    [Theory]
    [InlineData("en-US", "The club was not found.")]
    [InlineData("tr-TR", "Kulüp bulunamadı.")]
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
            OrganizationErrors.NotFound.Message.Should().Be("Kulüp bulunamadı.");

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            OrganizationErrors.NotFound.Message.Should().Be("The club was not found.");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Theory]
    [InlineData("en-US", "Organization_NotPublic", "This club is private. You need an invite code to join.")]
    [InlineData("tr-TR", "Organization_NotPublic", "Bu kulüp özel. Katılmak için davet kodu gerekir.")]
    [InlineData("en-US", "Organization_InvalidMedia", "Unsupported image type. Upload JPEG, PNG or WebP.")]
    [InlineData("tr-TR", "Organization_InvalidMedia", "Desteklenmeyen görsel türü. JPEG, PNG veya WebP yükle.")]
    public void NewClubResources_AreLocalized(string cultureName, string key, string expected)
    {
        var value = ErrorMessagesResource.ResourceManager.GetString(
            key,
            CultureInfo.GetCultureInfo(cultureName));

        value.Should().Be(expected);
    }

    [Fact]
    public void NotPublicAndInvalidMedia_HaveStableCodesAndMessages()
    {
        OrganizationErrors.NotPublic.Code.Should().Be("Organization.NotPublic");
        OrganizationErrors.NotPublic.Message.Should().NotBeNullOrWhiteSpace();
        OrganizationErrors.InvalidMedia.Code.Should().Be("Organization.InvalidMedia");
        OrganizationErrors.InvalidMedia.Message.Should().NotBeNullOrWhiteSpace();
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
