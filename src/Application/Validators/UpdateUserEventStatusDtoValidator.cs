using FluentValidation;
using Sportner.Application.DTOs.Events;
using Sportner.Localization.Resources;

namespace Sportner.Application.Validators;

public class UpdateUserEventStatusDtoValidator : AbstractValidator<UpdateUserEventStatusDto>
{
    public UpdateUserEventStatusDtoValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage(_ => ValidationResource.Validation_Status_Required);
    }
}
