using FluentAssertions;
using Sportner.Application.Features.Catalog.Sports.CreateSport;

namespace Sportner.Application.UnitTests.Features.Catalog.Sports;

public sealed class CreateSportCommandValidatorTests
{
    private readonly CreateSportCommandValidator _validator = new();

    [Fact]
    public void Validate_Passes_ForValidCommand()
    {
        var command = new CreateSportCommand("Basketball", DisplayOrder: 1, Slug: "basketball");

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Fails_WhenNameIsEmpty()
    {
        var command = new CreateSportCommand(string.Empty, DisplayOrder: 0);

        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenDisplayOrderIsNegative()
    {
        var command = new CreateSportCommand("Tennis", DisplayOrder: -1);

        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("Bad Slug")]
    [InlineData("-leading")]
    [InlineData("trailing-")]
    [InlineData("UPPER")]
    public void Validate_Fails_ForInvalidSlug(string slug)
    {
        var command = new CreateSportCommand("Tennis", DisplayOrder: 1, Slug: slug);

        _validator.Validate(command).IsValid.Should().BeFalse();
    }
}
