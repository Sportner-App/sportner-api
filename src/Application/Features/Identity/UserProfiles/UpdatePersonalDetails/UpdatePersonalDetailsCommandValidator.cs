using FluentValidation;

namespace Sportner.Application.Features.Identity.UserProfiles.UpdatePersonalDetails;

public sealed class UpdatePersonalDetailsCommandValidator
    : AbstractValidator<UpdatePersonalDetailsCommand>
{
    private const int MinimumAgeYears = 13;
    private const int MaximumAgeYears = 120;

    public UpdatePersonalDetailsCommandValidator(TimeProvider timeProvider)
    {
        // Gender stays a plain code until product approves an enum (docs/database/02-profiles.md).
        RuleFor(command => command.Gender)
            .InclusiveBetween((short)0, (short)2)
            .When(command => command.Gender is not null);

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        RuleFor(command => command.BirthDate)
            .Must(birthDate => birthDate <= today.AddYears(-MinimumAgeYears))
            .WithMessage($"Users must be at least {MinimumAgeYears} years old.")
            .Must(birthDate => birthDate >= today.AddYears(-MaximumAgeYears))
            .WithMessage("Birth date is not plausible.")
            .When(command => command.BirthDate is not null);
    }
}
