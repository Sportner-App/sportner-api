using FluentValidation;

namespace Sportner.Application.Features.Identity.Auth.CompleteExternalRegistration;

internal sealed class CompleteExternalRegistrationCommandValidator
    : AbstractValidator<CompleteExternalRegistrationCommand>
{
    public CompleteExternalRegistrationCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(command => command.RegistrationToken).NotEmpty().MaximumLength(4096);
        RuleFor(command => command.Username)
            .NotEmpty().MinimumLength(3).MaximumLength(30)
            .Matches(@"^[a-zA-Z0-9._]+$");
        RuleFor(command => command.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(command => command.LastName).MaximumLength(50)
            .When(command => !string.IsNullOrWhiteSpace(command.LastName));
        RuleFor(command => command.Gender).InclusiveBetween((short)0, (short)2);

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        RuleFor(command => command.BirthDate)
            .LessThanOrEqualTo(today.AddYears(-13))
            .WithMessage("Users must be at least 13 years old.")
            .GreaterThanOrEqualTo(today.AddYears(-120))
            .WithMessage("Birth date is not plausible.");
    }
}
