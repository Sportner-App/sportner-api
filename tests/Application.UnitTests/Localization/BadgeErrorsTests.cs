using System.Globalization;
using FluentAssertions;
using Sportner.Application.Features.Gamification;
using Sportner.Localization.Resources;

namespace Sportner.Application.UnitTests.Localization;

public class BadgeErrorsTests
{
    [Theory]
    [InlineData("en-US", "Only earned badges can be showcased.")]
    [InlineData("tr-TR", "Vitrinde yalnızca kazandığın rozetler gösterilebilir.")]
    public void ShowcaseNotOwnedResource_IsLocalized(string cultureName, string expected)
    {
        var value = ErrorMessagesResource.ResourceManager.GetString(
            "Badge_ShowcaseNotOwned",
            CultureInfo.GetCultureInfo(cultureName));

        value.Should().Be(expected);
    }

    [Fact]
    public void ShowcaseNotOwned_ReflectsCurrentUICultureOnEachAccess()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
            BadgeErrors.ShowcaseNotOwned.Message.Should().Be("Vitrinde yalnızca kazandığın rozetler gösterilebilir.");

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            BadgeErrors.ShowcaseNotOwned.Message.Should().Be("Only earned badges can be showcased.");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public void ShowcaseNotOwned_CodeStaysStableAcrossCultures()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
            var trCode = BadgeErrors.ShowcaseNotOwned.Code;

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var enCode = BadgeErrors.ShowcaseNotOwned.Code;

            trCode.Should().Be("Badge.ShowcaseNotOwned");
            enCode.Should().Be("Badge.ShowcaseNotOwned");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Theory]
    [InlineData("en-US", "At most 3 badges can be showcased.")]
    [InlineData("tr-TR", "En fazla 3 rozet vitrinde sergilenebilir.")]
    public void ShowcaseTooMany_FormatsPlaceholder(string cultureName, string expected)
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
            BadgeErrors.ShowcaseTooMany.Message.Should().Be(expected);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}
