using Sportner.Domain.Sports;

namespace Sportner.Application.Features.Catalog.Sports;

public sealed record SportResponse(
    Guid Id,
    string Name,
    string Slug,
    string? IconUrl,
    string? CoverImageUrl,
    int DisplayOrder,
    Guid? CategoryId,
    string? CategoryName = null,
    string? CategorySlug = null)
{
    public static SportResponse From(Sport sport, SportCategory? category = null) =>
        new(
            sport.Id,
            sport.Name,
            sport.Slug,
            sport.IconUrl,
            sport.CoverImageUrl,
            sport.DisplayOrder,
            sport.CategoryId,
            category?.Name,
            category?.Slug);
}

public sealed record SportCategoryResponse(
    Guid Id,
    string Name,
    string Slug,
    int DisplayOrder,
    int SportCount)
{
    public static SportCategoryResponse From(SportCategory category, int sportCount) =>
        new(category.Id, category.Name, category.Slug, category.DisplayOrder, sportCount);
}
