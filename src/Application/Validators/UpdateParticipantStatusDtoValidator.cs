using FluentValidation;
using Sportner.Application.DTOs.Events;
using Sportner.Localization.Resources;

namespace Sportner.Application.Validators;

public class UpdateParticipantStatusDtoValidator : AbstractValidator<UpdateParticipantStatusDto>
{
    public UpdateParticipantStatusDtoValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage(_ => ValidationResource.Validation_Status_Required);
    }
}
