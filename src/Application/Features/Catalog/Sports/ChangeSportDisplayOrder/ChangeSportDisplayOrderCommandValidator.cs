using FluentValidation;

namespace Sportner.Application.Features.Catalog.Sports.ChangeSportDisplayOrder;

public sealed class ChangeSportDisplayOrderCommandValidator
    : AbstractValidator<ChangeSportDisplayOrderCommand>
{
    public ChangeSportDisplayOrderCommandValidator()
    {
        RuleFor(command => command.SportId).NotEmpty();
        RuleFor(command => command.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}
