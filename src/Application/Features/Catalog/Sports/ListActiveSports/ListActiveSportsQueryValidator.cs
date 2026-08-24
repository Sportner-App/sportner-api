using FluentValidation;

namespace Sportner.Application.Features.Catalog.Sports.ListActiveSports;

public sealed class ListActiveSportsQueryValidator : AbstractValidator<ListActiveSportsQuery>
{
    public const int MinSearchLength = 2;
    public const int MaxSearchLength = 50;

    public ListActiveSportsQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, PaginationMaxPageSize);

        RuleFor(query => query.Search)
            .Must(BeValidSearch)
            .WithMessage($"Search must be empty or between {MinSearchLength} and {MaxSearchLength} characters.");
    }

    private const int PaginationMaxPageSize = 100;

    private static bool BeValidSearch(string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return true;
        }

        var length = search.Trim().Length;
        return length is >= MinSearchLength and <= MaxSearchLength;
    }
}
