using FluentValidation;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Identity.Auth.UpdatePreferredLanguage;

public sealed class UpdatePreferredLanguageCommandValidator
    : AbstractValidator<UpdatePreferredLanguageCommand>
{
    public UpdatePreferredLanguageCommandValidator()
    {
        RuleFor(command => command.Language)
            .Must(value => Enum.IsDefined(value))
            .WithMessage("Language is invalid.");
    }
}
