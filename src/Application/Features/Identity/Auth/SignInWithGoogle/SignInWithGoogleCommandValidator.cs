using FluentValidation;

namespace Sportner.Application.Features.Identity.Auth.SignInWithGoogle;

internal sealed class SignInWithGoogleCommandValidator : AbstractValidator<SignInWithGoogleCommand>
{
    public SignInWithGoogleCommandValidator()
    {
        RuleFor(command => command.IdToken)
            .NotEmpty();
    }
}
