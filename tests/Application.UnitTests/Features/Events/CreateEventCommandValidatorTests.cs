using FluentAssertions;
using Sportner.Application.Features.Events.CreateEvent;

namespace Sportner.Application.UnitTests.Features.Events;

public sealed class CreateEventCommandValidatorTests
{
    private readonly CreateEventCommandValidator _validator = new();

    private static CreateEventCommand ValidCommand() =>
        new(
            SportId: Guid.NewGuid(),
            Title: "Pazar sabahı basketbol",
            Description: "Dostluk maçı",
            EventDate: DateTimeOffset.UtcNow.AddDays(3),
            DurationMinutes: 90,
            Latitude: 41.015137m,
            Longitude: 28.979530m,
            Address: "Kadıköy Spor Salonu, İstanbul",
            MaxParticipants: 6);

    [Fact]
    public void Validate_Passes_ForValidCommand()
    {
        _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Fails_WhenTitleIsEmpty()
    {
        _validator.Validate(ValidCommand() with { Title = "" }).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenDurationIsNotPositive()
    {
        _validator.Validate(ValidCommand() with { DurationMinutes = 0 }).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(-91)]
    [InlineData(91)]
    public void Validate_Fails_ForOutOfRangeLatitude(double latitude)
    {
        _validator.Validate(ValidCommand() with { Latitude = (decimal)latitude })
            .IsValid.Should().BeFalse();
    }
}
