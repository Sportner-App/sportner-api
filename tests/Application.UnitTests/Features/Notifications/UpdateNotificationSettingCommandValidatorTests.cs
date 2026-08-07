using FluentAssertions;
using Sportner.Application.Features.Notifications.UpdateNotificationSetting;

namespace Sportner.Application.UnitTests.Features.Notifications;

public sealed class UpdateNotificationSettingCommandValidatorTests
{
    private readonly UpdateNotificationSettingCommandValidator _validator = new();

    [Fact]
    public void Validate_Passes_ForDefinedType()
    {
        var command = new UpdateNotificationSettingCommand(1, true, true, false);

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Fails_ForUndefinedType()
    {
        var command = new UpdateNotificationSettingCommand(999, true, false, false);

        _validator.Validate(command).IsValid.Should().BeFalse();
    }
}
