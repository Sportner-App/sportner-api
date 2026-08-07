using FluentValidation;

namespace Sportner.Application.Features.Identity.Profiles.UpdateUsername;

public sealed class UpdateUsernameCommandValidator : AbstractValidator<UpdateUsernameCommand>
{
    public UpdateUsernameCommandValidator()
    {
        RuleFor(command => command.Username)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(30)
            .Matches("^[a-zA-Z0-9._]+$")
            .WithMessage("Username can only contain letters, digits, dots and underscores.");
    }
}
