using FluentValidation;

namespace Sportner.Application.Features.Identity.Auth.VerifyOtp;

public sealed class VerifyOtpCommandValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpCommandValidator()
    {
        RuleFor(command => command.PhoneNumber)
            .NotEmpty()
            .MaximumLength(20)
            .Matches(@"^\+?[1-9]\d{6,14}$")
            .WithMessage("Phone number must be a valid international number.");

        RuleFor(command => command.Code)
            .NotEmpty()
            .Matches(@"^\d{4,8}$")
            .WithMessage("Verification code must be 4 to 8 digits.");
    }
}
