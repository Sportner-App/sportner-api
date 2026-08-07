using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Catalog.Sports.ListActiveSports;

internal sealed class ListActiveSportsQueryHandler
    : IQueryHandler<ListActiveSportsQuery, IReadOnlyList<SportResponse>>
{
    private readonly IApplicationDbContext _dbContext;

    public ListActiveSportsQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<SportResponse>>> Handle(
        ListActiveSportsQuery request,
        CancellationToken cancellationToken)
    {
        var sports = await _dbContext.Sports.AsNoTracking()
            .Where(sport => sport.IsActive)
            .OrderBy(sport => sport.DisplayOrder)
            .Select(sport => new SportResponse(
                sport.Id,
                sport.Name,
                sport.Slug,
                sport.IconUrl,
                sport.DisplayOrder))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<SportResponse>>.Success(sports);
    }
}
