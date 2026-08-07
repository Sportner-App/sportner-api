using FluentAssertions;
using Sportner.Application.Features.Identity.UserProfiles.CreateProfile;

namespace Sportner.Application.UnitTests.Features.Identity.UserProfiles;

public sealed class CreateProfileCommandValidatorTests
{
    private readonly CreateProfileCommandValidator _validator = new();

    [Fact]
    public void Validate_Passes_ForValidCommand()
    {
        var command = new CreateProfileCommand(
            "ahmet.yilmaz",
            "Ahmet",
            "Yılmaz",
            "Basketbol ve koşu.",
            "İstanbul",
            IsProfilePublic: true);

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Ahmet")]
    [InlineData("ab", "Ahmet")]
    [InlineData("ahmet yilmaz", "Ahmet")]
    [InlineData("ahmet@yilmaz", "Ahmet")]
    [InlineData("ahmet", "")]
    public void Validate_Fails_ForInvalidUsernameOrFirstName(string username, string firstName)
    {
        var command = new CreateProfileCommand(
            username,
            firstName,
            LastName: null,
            Bio: null,
            City: null,
            IsProfilePublic: true);

        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenBioExceedsLimit()
    {
        var command = new CreateProfileCommand(
            "ahmet",
            "Ahmet",
            LastName: null,
            Bio: new string('a', 501),
            City: null,
            IsProfilePublic: true);

        _validator.Validate(command).IsValid.Should().BeFalse();
    }
}
