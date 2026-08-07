using FluentValidation;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Identity.UserSports.ChangeSportSkillLevel;

public sealed class ChangeSportSkillLevelCommandValidator
    : AbstractValidator<ChangeSportSkillLevelCommand>
{
    public ChangeSportSkillLevelCommandValidator()
    {
        RuleFor(command => command.SportId)
            .NotEmpty();

        RuleFor(command => command.SkillLevel)
            .Must(level => Enum.IsDefined((SkillLevel)level))
            .WithMessage("Skill level is invalid.");
    }
}
