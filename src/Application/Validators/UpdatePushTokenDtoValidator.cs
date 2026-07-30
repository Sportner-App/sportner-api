using FluentValidation;
using Sportner.Application.DTOs.Auth;
using Sportner.Localization.Resources;

namespace Sportner.Application.Validators;

public class UpdatePushTokenDtoValidator : AbstractValidator<UpdatePushTokenDto>
{
    public UpdatePushTokenDtoValidator()
    {
        RuleFor(x => x.PushToken)
            .NotEmpty().WithMessage(_ => ValidationResource.Validation_PushToken_Required);
    }
}
