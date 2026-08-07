using FluentValidation;

namespace Sportner.Application.Features.Identity.UserProfiles.CreateProfile;

public sealed class CreateProfileCommandValidator : AbstractValidator<CreateProfileCommand>
{
    public CreateProfileCommandValidator()
    {
        RuleFor(command => command.Username)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(30)
            .Matches("^[a-zA-Z0-9._]+$")
            .WithMessage("Username can only contain letters, digits, dots and underscores.");

        RuleFor(command => command.FirstName)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(command => command.LastName)
            .MaximumLength(50);

        RuleFor(command => command.Bio)
            .MaximumLength(500);

        RuleFor(command => command.City)
            .MaximumLength(100);
    }
}
