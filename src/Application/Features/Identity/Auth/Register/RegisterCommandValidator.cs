using FluentValidation;

namespace Sportner.Application.Features.Identity.Auth.Register;

internal sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator(TimeProvider timeProvider)
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

        RuleFor(command => command.Email)
            .NotEmpty()
            .MaximumLength(254)
            .EmailAddress();

        RuleFor(command => command.FirstName)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(command => command.LastName)
            .NotEmpty()
            .MaximumLength(50);

        // Gender and birth date are optional (App Store 5.1.1(v)); validated only when given.
        RuleFor(command => command.Gender)
            .InclusiveBetween((short)0, (short)2)
            .When(command => command.Gender is not null);

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        RuleFor(command => command.BirthDate)
            .LessThanOrEqualTo(today.AddYears(-13))
            .WithMessage("Users must be at least 13 years old.")
            .GreaterThanOrEqualTo(today.AddYears(-120))
            .WithMessage("Birth date is not plausible.")
            .When(command => command.BirthDate is not null);
    }
}
