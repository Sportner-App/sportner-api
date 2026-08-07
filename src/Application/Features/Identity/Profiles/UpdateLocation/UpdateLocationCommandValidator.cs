using FluentValidation;

namespace Sportner.Application.Features.Identity.Profiles.UpdateLocation;

public sealed class UpdateLocationCommandValidator : AbstractValidator<UpdateLocationCommand>
{
    public UpdateLocationCommandValidator()
    {
        RuleFor(command => command.City)
            .MaximumLength(100);
    }
}
