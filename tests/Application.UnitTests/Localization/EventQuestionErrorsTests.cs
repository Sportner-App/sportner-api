using System.Globalization;
using FluentAssertions;
using Sportner.Application.Features.Events.EventQuestions;
using Sportner.Localization.Resources;

namespace Sportner.Application.UnitTests.Localization;

public class EventQuestionErrorsTests
{
    [Theory]
    [InlineData("en-US", "The question was not found.")]
    [InlineData("tr-TR", "Soru bulunamadı.")]
    public void QuestionNotFoundResource_IsLocalized(string cultureName, string expected)
    {
        var value = ErrorMessagesResource.ResourceManager.GetString(
            "EventQuestion_NotFound",
            CultureInfo.GetCultureInfo(cultureName));

        value.Should().Be(expected);
    }

    [Fact]
    public void QuestionNotFound_ReflectsCurrentUICultureOnEachAccess()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
            EventQuestionErrors.QuestionNotFound.Message.Should().Be("Soru bulunamadı.");

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            EventQuestionErrors.QuestionNotFound.Message.Should().Be("The question was not found.");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public void QuestionNotFound_CodeStaysStableAcrossCultures()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
            var trCode = EventQuestionErrors.QuestionNotFound.Code;

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var enCode = EventQuestionErrors.QuestionNotFound.Code;

            trCode.Should().Be("EventQuestion.NotFound");
            enCode.Should().Be("EventQuestion.NotFound");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}
