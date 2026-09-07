using System.Globalization;
using FluentAssertions;
using Sportner.Application.Features.Identity.Devices;
using Sportner.Localization.Resources;

namespace Sportner.Application.UnitTests.Localization;

public class DeviceErrorsTests
{
    [Theory]
    [InlineData("en-US", "The device was not found.")]
    [InlineData("tr-TR", "Cihaz bulunamadı.")]
    public void NotFoundResource_IsLocalized(string cultureName, string expected)
    {
        var value = ErrorMessagesResource.ResourceManager.GetString(
            "Device_NotFound",
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
            DeviceErrors.NotFound.Message.Should().Be("Cihaz bulunamadı.");

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            DeviceErrors.NotFound.Message.Should().Be("The device was not found.");
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
            var trCode = DeviceErrors.NotFound.Code;

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var enCode = DeviceErrors.NotFound.Code;

            trCode.Should().Be("Device.NotFound");
            enCode.Should().Be("Device.NotFound");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}
