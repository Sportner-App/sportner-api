using System.Globalization;
using FluentAssertions;
using Sportner.Application.Common.Localization;

namespace Sportner.Application.UnitTests.Common.Localization;

public class CatalogLocalizationTests
{
    [Fact]
    public void Resolve_ReturnsEnglishText_WhenCultureIsEnglishAndEnglishTextProvided()
    {
        WithCulture("en-US", () =>
            CatalogLocalization.Resolve("Futbol", "Football").Should().Be("Football"));
    }

    [Fact]
    public void Resolve_ReturnsDefaultText_WhenCultureIsTurkish()
    {
        WithCulture("tr-TR", () =>
            CatalogLocalization.Resolve("Futbol", "Football").Should().Be("Futbol"));
    }

    [Fact]
    public void Resolve_FallsBackToDefaultText_WhenEnglishTextIsMissing()
    {
        WithCulture("en-US", () =>
            CatalogLocalization.Resolve("Futbol", null).Should().Be("Futbol"));
    }

    [Fact]
    public void Resolve_FallsBackToDefaultText_WhenEnglishTextIsWhitespace()
    {
        WithCulture("en-US", () =>
            CatalogLocalization.Resolve("Futbol", "   ").Should().Be("Futbol"));
    }

    private static void WithCulture(string cultureName, Action assertion)
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
            assertion();
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}
