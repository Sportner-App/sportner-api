using FluentValidation;

namespace Sportner.Application.Features.Catalog.Sports.DeactivateSport;

public sealed class DeactivateSportCommandValidator : AbstractValidator<DeactivateSportCommand>
{
    public DeactivateSportCommandValidator()
    {
        RuleFor(command => command.SportId).NotEmpty();
    }
}
