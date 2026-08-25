using FluentValidation;

namespace Sportner.Application.Features.Events.ListMyParticipatingEvents;

public sealed class ListMyParticipatingEventsQueryValidator
    : AbstractValidator<ListMyParticipatingEventsQuery>
{
    private static readonly string[] AllowedScopes = ["upcoming", "past"];

    public ListMyParticipatingEventsQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.Scope)
            .Must(scope =>
                string.IsNullOrWhiteSpace(scope)
                || AllowedScopes.Contains(scope.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage("Scope must be empty, 'upcoming', or 'past'.");
    }
}
