using System.Globalization;
using FluentAssertions;
using Sportner.Application.Features.Messaging;
using Sportner.Localization.Resources;

namespace Sportner.Application.UnitTests.Localization;

public class MessagingErrorsTests
{
    [Theory]
    [InlineData("en-US", "The conversation was not found.")]
    [InlineData("tr-TR", "Sohbet bulunamadı.")]
    public void ConversationNotFoundResource_IsLocalized(string cultureName, string expected)
    {
        var value = ErrorMessagesResource.ResourceManager.GetString(
            "Messaging_ConversationNotFound",
            CultureInfo.GetCultureInfo(cultureName));

        value.Should().Be(expected);
    }

    [Fact]
    public void ConversationNotFound_ReflectsCurrentUICultureOnEachAccess()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
            MessagingErrors.ConversationNotFound.Message.Should().Be("Sohbet bulunamadı.");

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            MessagingErrors.ConversationNotFound.Message.Should().Be("The conversation was not found.");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public void ConversationNotFound_CodeStaysStableAcrossCultures()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
            var trCode = MessagingErrors.ConversationNotFound.Code;

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var enCode = MessagingErrors.ConversationNotFound.Code;

            trCode.Should().Be("Messaging.ConversationNotFound");
            enCode.Should().Be("Messaging.ConversationNotFound");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}
