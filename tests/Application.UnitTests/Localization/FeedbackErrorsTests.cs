using System.Globalization;
using FluentAssertions;
using Sportner.Application.Features.Feedback;
using Sportner.Localization.Resources;

namespace Sportner.Application.UnitTests.Localization;

public class FeedbackErrorsTests
{
    [Theory]
    [InlineData("en-US", "Feedback content is invalid.")]
    [InlineData("tr-TR", "Geri bildirim metni geçersiz.")]
    public void InvalidContentResource_IsLocalized(string cultureName, string expected)
    {
        var value = ErrorMessagesResource.ResourceManager.GetString(
            "AppFeedback_InvalidContent",
            CultureInfo.GetCultureInfo(cultureName));

        value.Should().Be(expected);
    }

    [Fact]
    public void InvalidContent_ReflectsCurrentUICultureOnEachAccess()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
            FeedbackErrors.InvalidContent.Message.Should().Be("Geri bildirim metni geçersiz.");

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            FeedbackErrors.InvalidContent.Message.Should().Be("Feedback content is invalid.");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public void InvalidContent_CodeStaysStableAcrossCultures()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
            var trCode = FeedbackErrors.InvalidContent.Code;

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var enCode = FeedbackErrors.InvalidContent.Code;

            trCode.Should().Be("AppFeedback.InvalidContent");
            enCode.Should().Be("AppFeedback.InvalidContent");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}
