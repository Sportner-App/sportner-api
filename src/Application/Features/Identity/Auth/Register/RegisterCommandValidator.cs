using FluentValidation;

namespace Sportner.Application.Features.Identity.Auth.Register;

internal sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(command => command.Username)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(30)
            .Matches(@"^[a-zA-Z0-9._]+$");

        RuleFor(command => command.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128);

        RuleFor(command => command.FirstName)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(command => command.LastName)
            .MaximumLength(50)
            .When(command => !string.IsNullOrWhiteSpace(command.LastName));
    }
}
