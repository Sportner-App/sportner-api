using FluentValidation;

namespace Sportner.Application.Features.Identity.UserProfiles.UpdateDisplayName;

public sealed class UpdateDisplayNameCommandValidator : AbstractValidator<UpdateDisplayNameCommand>
{
    public UpdateDisplayNameCommandValidator()
    {
        RuleFor(command => command.FirstName)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(command => command.LastName)
            .MaximumLength(50);
    }
}
