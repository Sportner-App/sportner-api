using FluentValidation;

namespace Sportner.Application.Features.Identity.Auth.Logout;

public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(command => command.RefreshToken)
            .NotEmpty();
    }
}
