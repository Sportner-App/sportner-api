using FluentValidation;

namespace Sportner.Application.Features.Identity.UserProfiles.UpdateBio;

public sealed class UpdateBioCommandValidator : AbstractValidator<UpdateBioCommand>
{
    public UpdateBioCommandValidator()
    {
        RuleFor(command => command.Bio)
            .MaximumLength(500);
    }
}
