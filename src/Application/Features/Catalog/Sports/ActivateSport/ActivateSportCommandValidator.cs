using FluentValidation;

namespace Sportner.Application.Features.Catalog.Sports.ActivateSport;

public sealed class ActivateSportCommandValidator : AbstractValidator<ActivateSportCommand>
{
    public ActivateSportCommandValidator()
    {
        RuleFor(command => command.SportId).NotEmpty();
    }
}
