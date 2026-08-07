using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Sportner.Application.Features.Identity.Profiles.UpdatePersonalDetails;

namespace Sportner.Application.UnitTests.Features.Identity.Profiles;

public sealed class UpdatePersonalDetailsCommandValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 6, 12, 0, 0, TimeSpan.Zero);

    private readonly UpdatePersonalDetailsCommandValidator _validator =
        new(new FakeTimeProvider(Now));

    [Fact]
    public void Validate_Passes_WhenBothValuesAreNull()
    {
        _validator.Validate(new UpdatePersonalDetailsCommand(null, null)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Passes_ForAdultBirthDate()
    {
        var command = new UpdatePersonalDetailsCommand(1, new DateOnly(1995, 4, 12));

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Fails_WhenUserIsUnderThirteen()
    {
        var command = new UpdatePersonalDetailsCommand(null, new DateOnly(2020, 1, 1));

        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_ForImplausibleBirthDate()
    {
        var command = new UpdatePersonalDetailsCommand(null, new DateOnly(1850, 1, 1));

        _validator.Validate(command).IsValid.Should().BeFalse();
    }
}
