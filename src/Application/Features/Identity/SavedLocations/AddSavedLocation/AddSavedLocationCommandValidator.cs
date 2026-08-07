using FluentValidation;

namespace Sportner.Application.Features.Identity.SavedLocations.AddSavedLocation;

public sealed class AddSavedLocationCommandValidator : AbstractValidator<AddSavedLocationCommand>
{
    public AddSavedLocationCommandValidator()
    {
        RuleFor(command => command.Title)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.Latitude)
            .InclusiveBetween(-90m, 90m);

        RuleFor(command => command.Longitude)
            .InclusiveBetween(-180m, 180m);

        RuleFor(command => command.Address)
            .NotEmpty();

        RuleFor(command => command.City)
            .MaximumLength(100);

        RuleFor(command => command.District)
            .MaximumLength(100);
    }
}
