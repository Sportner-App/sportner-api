using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Catalog.Cities.ListCities;

public sealed record ListCitiesQuery : IQuery<IReadOnlyList<CityResponse>>;

public sealed record CityResponse(Guid Id, short PlateCode, string Name);
