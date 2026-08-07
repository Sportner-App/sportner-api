using FluentValidation;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Identity.UserSports.AddSport;

public sealed class AddSportCommandValidator : AbstractValidator<AddSportCommand>
{
    public AddSportCommandValidator()
    {
        RuleFor(command => command.SportId)
            .NotEmpty();

        RuleFor(command => command.SkillLevel)
            .Must(level => Enum.IsDefined((SkillLevel)level))
            .WithMessage("Skill level is invalid.");
    }
}
