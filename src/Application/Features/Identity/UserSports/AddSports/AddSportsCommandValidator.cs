using FluentValidation;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Identity.UserSports.AddSports;

public sealed class AddSportsCommandValidator : AbstractValidator<AddSportsCommand>
{
    public AddSportsCommandValidator()
    {
        RuleFor(command => command.Sports)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .NotEmpty()
            .Must(sports => sports.Count <= 20)
            .WithMessage("At most 20 sports can be added at once.")
            .Must(sports => sports.Select(item => item.SportId).Distinct().Count() == sports.Count)
            .WithMessage("The same sport cannot be included more than once.")
            .Must(sports => sports.Count(item => item.IsPrimary) <= 1)
            .WithMessage("Only one sport can be primary.");

        RuleForEach(command => command.Sports).ChildRules(item =>
        {
            item.RuleFor(value => value.SportId).NotEmpty();
            item.RuleFor(value => value.SkillLevel)
                .Must(level => Enum.IsDefined((SkillLevel)level))
                .WithMessage("Skill level is invalid.");
        });
    }
}
