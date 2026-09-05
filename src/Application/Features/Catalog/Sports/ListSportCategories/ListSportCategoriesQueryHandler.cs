using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Catalog.Sports.ListSportCategories;

internal sealed class ListSportCategoriesQueryHandler
    : IQueryHandler<ListSportCategoriesQuery, IReadOnlyList<SportCategoryResponse>>
{
    private readonly IApplicationDbContext _dbContext;

    public ListSportCategoriesQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<SportCategoryResponse>>> Handle(
        ListSportCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _dbContext.SportCategories.AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .Select(category => new SportCategoryResponse(
                category.Id,
                category.Name,
                category.Slug,
                category.DisplayOrder,
                _dbContext.Sports.Count(sport =>
                    sport.CategoryId == category.Id && sport.IsActive)))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<SportCategoryResponse>>.Success(items);
    }
}
