using FluentValidation;

namespace Sportner.Application.Features.Identity.Auth.RequestOtp;

public sealed class RequestOtpCommandValidator : AbstractValidator<RequestOtpCommand>
{
    public RequestOtpCommandValidator()
    {
        RuleFor(command => command.PhoneNumber)
            .NotEmpty()
            .MaximumLength(20)
            .Matches(@"^\+?[1-9]\d{6,14}$")
            .WithMessage("Phone number must be a valid international number.");
    }
}
