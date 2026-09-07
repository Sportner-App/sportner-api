using System.Globalization;
using FluentAssertions;
using Sportner.Application.Features.Explore;
using Sportner.Localization.Resources;

namespace Sportner.Application.UnitTests.Localization;

public class ExploreErrorsTests
{
    [Theory]
    [InlineData("en-US", "Authentication is required.")]
    [InlineData("tr-TR", "Bu işlem için giriş yapmalısın.")]
    public void NotAuthenticatedResource_IsLocalized(string cultureName, string expected)
    {
        var value = ErrorMessagesResource.ResourceManager.GetString(
            "Explore_NotAuthenticated",
            CultureInfo.GetCultureInfo(cultureName));

        value.Should().Be(expected);
    }

    [Fact]
    public void NotAuthenticated_ReflectsCurrentUICultureOnEachAccess()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
            ExploreErrors.NotAuthenticated.Message.Should().Be("Bu işlem için giriş yapmalısın.");

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            ExploreErrors.NotAuthenticated.Message.Should().Be("Authentication is required.");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public void NotAuthenticated_CodeStaysStableAcrossCultures()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
            var trCode = ExploreErrors.NotAuthenticated.Code;

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var enCode = ExploreErrors.NotAuthenticated.Code;

            trCode.Should().Be("Explore.NotAuthenticated");
            enCode.Should().Be("Explore.NotAuthenticated");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}
