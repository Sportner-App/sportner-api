using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Catalog.Sports.ListSportCategories;

public sealed record ListSportCategoriesQuery
    : IQuery<IReadOnlyList<SportCategoryResponse>>;
