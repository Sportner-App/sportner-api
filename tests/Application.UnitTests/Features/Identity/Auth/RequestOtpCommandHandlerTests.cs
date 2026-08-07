using FluentAssertions;
using Moq;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Features.Identity.Auth.RequestOtp;

namespace Sportner.Application.UnitTests.Features.Identity.Auth;

public sealed class RequestOtpCommandHandlerTests
{
    [Fact]
    public async Task Handle_RequestsOtp_ForTrimmedPhoneNumber_AndSucceeds()
    {
        var otpService = new Mock<IOtpService>();
        var handler = new RequestOtpCommandHandler(otpService.Object);

        var result = await handler.Handle(
            new RequestOtpCommand("  +905551112233  "),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        otpService.Verify(
            service => service.RequestAsync("+905551112233", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
