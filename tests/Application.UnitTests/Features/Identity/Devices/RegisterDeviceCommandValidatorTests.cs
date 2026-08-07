using FluentAssertions;
using Sportner.Application.Features.Identity.Devices.RegisterDevice;

namespace Sportner.Application.UnitTests.Features.Identity.Devices;

public sealed class RegisterDeviceCommandValidatorTests
{
    private readonly RegisterDeviceCommandValidator _validator = new();

    [Fact]
    public void Validate_Passes_ForValidCommand()
    {
        var command = new RegisterDeviceCommand(
            Platform: 1,
            DeviceIdentifier: "device-123",
            DeviceName: "Pixel 8",
            AppVersion: "1.0.0",
            OsVersion: "Android 15",
            PushToken: "token");

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData((short)5)]
    [InlineData((short)-1)]
    public void Validate_Fails_ForUnknownPlatform(short platform)
    {
        var command = new RegisterDeviceCommand(
            platform,
            "device-123",
            DeviceName: null,
            AppVersion: null,
            OsVersion: null,
            PushToken: null);

        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenDeviceIdentifierIsEmpty()
    {
        var command = new RegisterDeviceCommand(
            Platform: 0,
            DeviceIdentifier: "",
            DeviceName: null,
            AppVersion: null,
            OsVersion: null,
            PushToken: null);

        _validator.Validate(command).IsValid.Should().BeFalse();
    }
}
