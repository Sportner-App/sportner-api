using FluentAssertions;
using Sportner.Application.Features.Messaging.SendTextMessage;

namespace Sportner.Application.UnitTests.Features.Messaging;

public sealed class SendTextMessageCommandValidatorTests
{
    private readonly SendTextMessageCommandValidator _validator = new();

    [Fact]
    public void Validate_Passes_ForValidCommand()
    {
        var command = new SendTextMessageCommand(Guid.NewGuid(), "Herkese merhaba");

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Fails_WhenContentIsEmpty()
    {
        var command = new SendTextMessageCommand(Guid.NewGuid(), "");

        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenConversationIdIsEmpty()
    {
        var command = new SendTextMessageCommand(Guid.Empty, "Merhaba");

        _validator.Validate(command).IsValid.Should().BeFalse();
    }
}
