using Sportner.Domain.Sports;

namespace Sportner.Application.Features.Catalog.Sports;

public sealed record SportResponse(
    Guid Id,
    string Name,
    string Slug,
    string? IconUrl,
    string? CoverImageUrl,
    int DisplayOrder)
{
    public static SportResponse From(Sport sport) =>
        new(
            sport.Id,
            sport.Name,
            sport.Slug,
            sport.IconUrl,
            sport.CoverImageUrl,
            sport.DisplayOrder);
}
