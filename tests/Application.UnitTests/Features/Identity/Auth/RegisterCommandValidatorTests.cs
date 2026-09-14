using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Sportner.Application.Features.Identity.Auth.Register;

namespace Sportner.Application.UnitTests.Features.Identity.Auth;

public sealed class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new(
        new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)));

    private static RegisterCommand ValidCommand(string? lastName = "Yılmaz") =>
        new(
            "ahmetx",
            "Password1!",
            "ahmet@example.com",
            "Ahmet",
            lastName,
            Gender: 0,
            BirthDate: new DateOnly(2000, 1, 1),
            IpAddress: null,
            UserAgent: null);

    [Fact]
    public void Validate_Passes_WhenLastNameIsProvided()
    {
        _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Fails_WhenLastNameIsMissing(string? lastName)
    {
        var result = _validator.Validate(ValidCommand(lastName));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(RegisterCommand.LastName));
    }
}
