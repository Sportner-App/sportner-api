using FluentValidation;

namespace Sportner.Application.Features.Catalog.Sports.RenameSport;

public sealed class RenameSportCommandValidator : AbstractValidator<RenameSportCommand>
{
    public RenameSportCommandValidator()
    {
        RuleFor(command => command.SportId).NotEmpty();

        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.Slug)
            .MaximumLength(100)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .When(command => !string.IsNullOrWhiteSpace(command.Slug));

        RuleFor(command => command.IconUrl)
            .MaximumLength(2048)
            .When(command => !string.IsNullOrWhiteSpace(command.IconUrl));
    }
}
