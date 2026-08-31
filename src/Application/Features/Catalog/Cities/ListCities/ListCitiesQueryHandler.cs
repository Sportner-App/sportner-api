using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Catalog.Cities.ListCities;

internal sealed class ListCitiesQueryHandler
    : IQueryHandler<ListCitiesQuery, IReadOnlyList<CityResponse>>
{
    private readonly IApplicationDbContext _dbContext;

    public ListCitiesQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<CityResponse>>> Handle(
        ListCitiesQuery request,
        CancellationToken cancellationToken)
    {
        var cities = await _dbContext.Cities
            .AsNoTracking()
            .OrderBy(city => city.PlateCode)
            .Select(city => new CityResponse(city.Id, city.PlateCode, city.Name))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<CityResponse>>.Success(cities);
    }
}
