using FluentValidation;
using Sportner.Application.DTOs.Messages;
using Sportner.Localization.Resources;

namespace Sportner.Application.Validators;

public class CreateMessageDtoValidator : AbstractValidator<CreateMessageDto>
{
    public CreateMessageDtoValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage(_ => ValidationResource.Validation_Content_Required)
            .MaximumLength(2000).WithMessage(_ => ValidationResource.Validation_Content_MaxLength);
    }
}
