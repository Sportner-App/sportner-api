using FluentAssertions;
using Sportner.Application.Features.Identity.UserSports.AddSports;

namespace Sportner.Application.UnitTests.Features.Identity.UserSports;

public sealed class AddSportsCommandValidatorTests
{
    private readonly AddSportsCommandValidator _validator = new();

    [Fact]
    public void Validate_Passes_ForDistinctValidSports()
    {
        var command = new AddSportsCommand([
            new AddSportsItem(Guid.NewGuid(), 1),
            new AddSportsItem(Guid.NewGuid(), 3),
        ]);

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Fails_ForDuplicateSportIds()
    {
        var sportId = Guid.NewGuid();
        var command = new AddSportsCommand([
            new AddSportsItem(sportId, 1),
            new AddSportsItem(sportId, 2),
        ]);

        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenMoreThanOneSportIsPrimary()
    {
        var command = new AddSportsCommand([
            new AddSportsItem(Guid.NewGuid(), 1, IsPrimary: true),
            new AddSportsItem(Guid.NewGuid(), 2, IsPrimary: true),
        ]);

        _validator.Validate(command).IsValid.Should().BeFalse();
    }
}
