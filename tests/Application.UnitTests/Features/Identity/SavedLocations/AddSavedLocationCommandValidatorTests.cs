using FluentAssertions;
using Sportner.Application.Features.Identity.SavedLocations.AddSavedLocation;

namespace Sportner.Application.UnitTests.Features.Identity.SavedLocations;

public sealed class AddSavedLocationCommandValidatorTests
{
    private readonly AddSavedLocationCommandValidator _validator = new();

    private static AddSavedLocationCommand ValidCommand() =>
        new(
            Title: "Ev",
            Latitude: 41.015137m,
            Longitude: 28.979530m,
            Address: "Kadıköy, İstanbul",
            City: "İstanbul",
            District: "Kadıköy",
            IsDefault: true);

    [Fact]
    public void Validate_Passes_ForValidCommand()
    {
        _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Fails_WhenTitleIsEmpty()
    {
        var command = ValidCommand() with { Title = "" };

        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(-91)]
    [InlineData(91)]
    public void Validate_Fails_ForOutOfRangeLatitude(double latitude)
    {
        var command = ValidCommand() with { Latitude = (decimal)latitude };

        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(-181)]
    [InlineData(181)]
    public void Validate_Fails_ForOutOfRangeLongitude(double longitude)
    {
        var command = ValidCommand() with { Longitude = (decimal)longitude };

        _validator.Validate(command).IsValid.Should().BeFalse();
    }
}
