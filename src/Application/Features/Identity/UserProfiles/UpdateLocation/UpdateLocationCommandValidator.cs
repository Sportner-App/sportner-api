using FluentValidation;

namespace Sportner.Application.Features.Identity.UserProfiles.UpdateLocation;

public sealed class UpdateLocationCommandValidator : AbstractValidator<UpdateLocationCommand>
{
    public UpdateLocationCommandValidator()
    {
        RuleFor(command => command.City)
            .MaximumLength(100);
    }
}
