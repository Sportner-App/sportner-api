using FluentAssertions;
using Sportner.Application.Features.Identity.Auth.VerifyOtp;

namespace Sportner.Application.UnitTests.Features.Identity.Auth;

public sealed class VerifyOtpCommandValidatorTests
{
    private readonly VerifyOtpCommandValidator _validator = new();

    [Fact]
    public void Validate_Passes_ForValidCommand()
    {
        var result = _validator.Validate(new VerifyOtpCommand("+905551112233", "123456"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("+905551112233", "")]
    [InlineData("+905551112233", "12")]
    [InlineData("+905551112233", "abcdef")]
    [InlineData("abc", "123456")]
    public void Validate_Fails_ForInvalidCommand(string phoneNumber, string code)
    {
        var result = _validator.Validate(new VerifyOtpCommand(phoneNumber, code));

        result.IsValid.Should().BeFalse();
    }
}
