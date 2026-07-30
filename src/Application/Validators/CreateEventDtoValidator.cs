using FluentValidation;
using Sportner.Application.DTOs.Events;
using Sportner.Localization.Resources;

namespace Sportner.Application.Validators;

public class CreateEventDtoValidator : AbstractValidator<CreateEventDto>
{
    public CreateEventDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage(_ => ValidationResource.Validation_Title_Required);

        RuleFor(x => x.SportType)
            .NotEmpty().WithMessage(_ => ValidationResource.Validation_SportType_Required);

        RuleFor(x => x.EventDate)
            .NotEmpty().WithMessage(_ => ValidationResource.Validation_EventDate_Required);

        RuleFor(x => x.MaxPlayers)
            .InclusiveBetween(2, 100).WithMessage(_ => ValidationResource.Validation_MaxPlayers_Range);

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90).WithMessage(_ => ValidationResource.Validation_Latitude_Required);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180).WithMessage(_ => ValidationResource.Validation_Longitude_Required);
    }
}
