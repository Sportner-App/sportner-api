using FluentValidation;
using Sportner.Application.DTOs.Auth;
using Sportner.Localization.Resources;

namespace Sportner.Application.Validators;

public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(_ => ValidationResource.Validation_Email_Required)
            .EmailAddress().WithMessage(_ => ValidationResource.Validation_Email_Invalid);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(_ => ValidationResource.Validation_Password_Required)
            .MinimumLength(6).WithMessage(_ => ValidationResource.Validation_Password_MinLength);

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage(_ => ValidationResource.Validation_FullName_Required);
    }
}
