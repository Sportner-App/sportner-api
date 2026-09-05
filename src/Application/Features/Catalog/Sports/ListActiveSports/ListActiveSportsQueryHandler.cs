using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Models;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Catalog.Sports.ListActiveSports;

internal sealed class ListActiveSportsQueryHandler
    : IQueryHandler<ListActiveSportsQuery, PagedResult<SportResponse>>
{
    private readonly IApplicationDbContext _dbContext;

    public ListActiveSportsQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PagedResult<SportResponse>>> Handle(
        ListActiveSportsQuery request,
        CancellationToken cancellationToken)
    {
        var pagination = new PaginationRequest(request.Page, request.PageSize);
        var search = string.IsNullOrWhiteSpace(request.Search)
            ? null
            : request.Search.Trim().ToLowerInvariant();

        var sports = _dbContext.Sports.AsNoTracking()
            .Where(sport => sport.IsActive);

        if (search is not null)
        {
            sports = sports.Where(sport =>
                sport.Name.ToLower().Contains(search)
                || sport.Slug.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(request.CategorySlug))
        {
            var categorySlug = request.CategorySlug.Trim().ToLowerInvariant();
            sports = sports.Where(sport => _dbContext.SportCategories.Any(category =>
                category.Id == sport.CategoryId && category.Slug == categorySlug));
        }

        var totalCount = await sports.CountAsync(cancellationToken);

        var items = await sports
            .OrderBy(sport => sport.DisplayOrder)
            .ThenBy(sport => sport.Name)
            .Skip(pagination.Skip)
            .Take(pagination.NormalizedPageSize)
            .Select(sport => new SportResponse(
                sport.Id,
                sport.Name,
                sport.Slug,
                sport.IconUrl,
                sport.CoverImageUrl,
                sport.DisplayOrder,
                sport.CategoryId,
                _dbContext.SportCategories
                    .Where(category => category.Id == sport.CategoryId)
                    .Select(category => category.Name)
                    .FirstOrDefault(),
                _dbContext.SportCategories
                    .Where(category => category.Id == sport.CategoryId)
                    .Select(category => category.Slug)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<SportResponse>>.Success(
            PagedResult<SportResponse>.Create(
                items,
                pagination.NormalizedPage,
                pagination.NormalizedPageSize,
                totalCount));
    }
}
