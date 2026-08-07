using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.Auth.RequestOtp;

internal sealed class RequestOtpCommandHandler : ICommandHandler<RequestOtpCommand>
{
    private readonly IOtpService _otpService;

    public RequestOtpCommandHandler(IOtpService otpService)
    {
        _otpService = otpService;
    }

    public async Task<Result> Handle(RequestOtpCommand request, CancellationToken cancellationToken)
    {
        var phoneNumber = request.PhoneNumber.Trim();

        // Always return success so the endpoint does not reveal whether a phone is registered.
        await _otpService.RequestAsync(phoneNumber, cancellationToken);

        return Result.Success();
    }
}
