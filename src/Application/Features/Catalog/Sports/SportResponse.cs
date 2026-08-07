namespace Sportner.Application.Features.Catalog.Sports;

public sealed record SportResponse(
    Guid Id,
    string Name,
    string Slug,
    string? IconUrl,
    int DisplayOrder);
