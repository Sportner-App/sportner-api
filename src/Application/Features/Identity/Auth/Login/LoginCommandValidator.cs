using FluentValidation;

namespace Sportner.Application.Features.Identity.Auth.Login;

internal sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.Username)
            .NotEmpty()
            .MaximumLength(30);

        RuleFor(command => command.Password)
            .NotEmpty()
            .MaximumLength(128);
    }
}
