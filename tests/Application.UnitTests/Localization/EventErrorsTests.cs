using System.Globalization;
using FluentAssertions;
using Sportner.Application.Features.Events;
using Sportner.Localization.Resources;

namespace Sportner.Application.UnitTests.Localization;

public class EventErrorsTests
{
    [Theory]
    [InlineData("en-US", "The event was not found.")]
    [InlineData("tr-TR", "Etkinlik bulunamadı.")]
    public void NotFoundResource_IsLocalized(string cultureName, string expected)
    {
        var value = ErrorMessagesResource.ResourceManager.GetString(
            "Event_NotFound",
            CultureInfo.GetCultureInfo(cultureName));

        value.Should().Be(expected);
    }

    /// <summary>
    /// EventErrors.NotFound bir <c>static readonly</c> alan değil, property
    /// olmalı — aksi halde mesaj yalnızca ilk erişimdeki
    /// <c>CurrentUICulture</c>'a göre sabitlenip her istekte aynı dili
    /// döndürür. Bu test aynı process içinde kültürü değiştirip her iki
    /// erişimin de güncel dile göre çözüldüğünü doğrular.
    /// </summary>
    [Fact]
    public void NotFound_ReflectsCurrentUICultureOnEachAccess()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
            EventErrors.NotFound.Message.Should().Be("Etkinlik bulunamadı.");

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            EventErrors.NotFound.Message.Should().Be("The event was not found.");
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
            var trCode = EventErrors.NotFound.Code;

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var enCode = EventErrors.NotFound.Code;

            trCode.Should().Be("Event.NotFound");
            enCode.Should().Be("Event.NotFound");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}
