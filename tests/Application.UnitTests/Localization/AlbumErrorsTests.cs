using System.Globalization;
using FluentAssertions;
using Sportner.Application.Features.Albums;
using Sportner.Localization.Resources;

namespace Sportner.Application.UnitTests.Localization;

public class AlbumErrorsTests
{
    [Theory]
    [InlineData("en-US", "The album was not found.")]
    [InlineData("tr-TR", "Albüm bulunamadı.")]
    public void NotFoundResource_IsLocalized(string cultureName, string expected)
    {
        var value = ErrorMessagesResource.ResourceManager.GetString(
            "Album_NotFound",
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
            AlbumErrors.NotFound.Message.Should().Be("Albüm bulunamadı.");

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            AlbumErrors.NotFound.Message.Should().Be("The album was not found.");
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
            var trCode = AlbumErrors.NotFound.Code;

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var enCode = AlbumErrors.NotFound.Code;

            trCode.Should().Be("Album.NotFound");
            enCode.Should().Be("Album.NotFound");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Theory]
    [InlineData("en-US", "A profile may have at most 20 albums.")]
    [InlineData("tr-TR", "Profilde en fazla 20 albüm olabilir.")]
    public void ProfileAlbumLimit_FormatsPlaceholder(string cultureName, string expected)
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
            AlbumErrors.ProfileAlbumLimit.Message.Should().Be(expected);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}
