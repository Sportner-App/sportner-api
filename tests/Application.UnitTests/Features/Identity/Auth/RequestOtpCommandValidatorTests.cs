using FluentAssertions;
using Sportner.Application.Features.Identity.Auth.RequestOtp;

namespace Sportner.Application.UnitTests.Features.Identity.Auth;

public sealed class RequestOtpCommandValidatorTests
{
    private readonly RequestOtpCommandValidator _validator = new();

    [Theory]
    [InlineData("+905551112233")]
    [InlineData("905551112233")]
    public void Validate_Passes_ForValidPhoneNumbers(string phoneNumber)
    {
        var result = _validator.Validate(new RequestOtpCommand(phoneNumber));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("+0123")]
    public void Validate_Fails_ForInvalidPhoneNumbers(string phoneNumber)
    {
        var result = _validator.Validate(new RequestOtpCommand(phoneNumber));

        result.IsValid.Should().BeFalse();
    }
}
