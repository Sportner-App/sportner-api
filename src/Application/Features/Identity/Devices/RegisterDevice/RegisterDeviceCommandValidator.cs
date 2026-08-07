using FluentValidation;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Identity.Devices.RegisterDevice;

public sealed class RegisterDeviceCommandValidator : AbstractValidator<RegisterDeviceCommand>
{
    public RegisterDeviceCommandValidator()
    {
        RuleFor(command => command.Platform)
            .Must(platform => Enum.IsDefined((DevicePlatform)platform))
            .WithMessage("Device platform is invalid.");

        RuleFor(command => command.DeviceIdentifier)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(command => command.DeviceName)
            .MaximumLength(100);

        RuleFor(command => command.AppVersion)
            .MaximumLength(30);

        RuleFor(command => command.OsVersion)
            .MaximumLength(30);
    }
}
