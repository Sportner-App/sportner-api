using FluentValidation;

namespace Sportner.Application.Features.Catalog.Sports.CreateSport;

public sealed class CreateSportCommandValidator : AbstractValidator<CreateSportCommand>
{
    public CreateSportCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.DisplayOrder)
            .GreaterThanOrEqualTo(0);

        RuleFor(command => command.Slug)
            .MaximumLength(100)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .When(command => !string.IsNullOrWhiteSpace(command.Slug));

        RuleFor(command => command.IconUrl)
            .MaximumLength(2048)
            .When(command => !string.IsNullOrWhiteSpace(command.IconUrl));
    }
}
