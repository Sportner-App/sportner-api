using FluentAssertions;
using Sportner.Application.Features.Identity.UserSports.AddSport;

namespace Sportner.Application.UnitTests.Features.Identity.UserSports;

public sealed class AddSportCommandValidatorTests
{
    private readonly AddSportCommandValidator _validator = new();

    [Fact]
    public void Validate_Passes_ForValidCommand()
    {
        var command = new AddSportCommand(Guid.NewGuid(), SkillLevel: 2, IsPrimary: true);

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Fails_WhenSportIdIsEmpty()
    {
        var command = new AddSportCommand(Guid.Empty, SkillLevel: 0, IsPrimary: false);

        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData((short)5)]
    [InlineData((short)-1)]
    public void Validate_Fails_ForUnknownSkillLevel(short skillLevel)
    {
        var command = new AddSportCommand(Guid.NewGuid(), skillLevel, IsPrimary: false);

        _validator.Validate(command).IsValid.Should().BeFalse();
    }
}
