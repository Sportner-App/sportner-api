using System.Globalization;
using FluentAssertions;
using Sportner.Application.Features.Notifications;
using Sportner.Localization.Resources;

namespace Sportner.Application.UnitTests.Localization;

public class NotificationErrorsTests
{
    [Theory]
    [InlineData("en-US", "The notification was not found.")]
    [InlineData("tr-TR", "Bildirim bulunamadı.")]
    public void NotFoundResource_IsLocalized(string cultureName, string expected)
    {
        var value = ErrorMessagesResource.ResourceManager.GetString(
            "Notification_NotFound",
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
            NotificationErrors.NotFound.Message.Should().Be("Bildirim bulunamadı.");

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            NotificationErrors.NotFound.Message.Should().Be("The notification was not found.");
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
            var trCode = NotificationErrors.NotFound.Code;

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var enCode = NotificationErrors.NotFound.Code;

            trCode.Should().Be("Notification.NotFound");
            enCode.Should().Be("Notification.NotFound");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}
