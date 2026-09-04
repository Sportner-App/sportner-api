using FluentValidation;

namespace Sportner.Application.Features.Identity.Auth.SignInWithApple;

internal sealed class SignInWithAppleCommandValidator : AbstractValidator<SignInWithAppleCommand>
{
    public SignInWithAppleCommandValidator()
    {
        RuleFor(command => command.IdentityToken)
            .NotEmpty();

        RuleFor(command => command.FirstName)
            .MaximumLength(50);

        RuleFor(command => command.LastName)
            .MaximumLength(50);
    }
}
