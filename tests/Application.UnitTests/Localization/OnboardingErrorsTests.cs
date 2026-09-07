using System.Globalization;
using FluentAssertions;
using Sportner.Application.Features.Identity.Onboarding;
using Sportner.Localization.Resources;

namespace Sportner.Application.UnitTests.Localization;

public class OnboardingErrorsTests
{
    [Theory]
    [InlineData("en-US", "You need to create a profile before completing onboarding.")]
    [InlineData("tr-TR", "Onboarding'i tamamlamak için önce profil oluşturmalısın.")]
    public void ProfileRequiredResource_IsLocalized(string cultureName, string expected)
    {
        var value = ErrorMessagesResource.ResourceManager.GetString(
            "Onboarding_ProfileRequired",
            CultureInfo.GetCultureInfo(cultureName));

        value.Should().Be(expected);
    }

    [Fact]
    public void ProfileRequired_ReflectsCurrentUICultureOnEachAccess()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
            OnboardingErrors.ProfileRequired.Message.Should().Be("Onboarding'i tamamlamak için önce profil oluşturmalısın.");

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            OnboardingErrors.ProfileRequired.Message.Should().Be("You need to create a profile before completing onboarding.");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public void ProfileRequired_CodeStaysStableAcrossCultures()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
            var trCode = OnboardingErrors.ProfileRequired.Code;

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var enCode = OnboardingErrors.ProfileRequired.Code;

            trCode.Should().Be("Onboarding.ProfileRequired");
            enCode.Should().Be("Onboarding.ProfileRequired");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}
