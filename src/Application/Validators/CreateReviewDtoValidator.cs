using FluentValidation;
using Sportner.Application.DTOs.Reviews;
using Sportner.Localization.Resources;

namespace Sportner.Application.Validators;

public class CreateReviewDtoValidator : AbstractValidator<CreateReviewDto>
{
    public CreateReviewDtoValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty().WithMessage(_ => ValidationResource.Validation_EventId_Required);

        RuleFor(x => x.ReviewedId)
            .NotEmpty().WithMessage(_ => ValidationResource.Validation_ReviewedId_Required);

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage(_ => ValidationResource.Validation_Rating_Range);

        RuleFor(x => x.Comment)
            .MaximumLength(1000)
            .When(x => x.Comment is not null);
    }
}
