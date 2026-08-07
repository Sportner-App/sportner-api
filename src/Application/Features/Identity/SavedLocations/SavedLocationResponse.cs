namespace Sportner.Application.Features.Identity.SavedLocations;

public sealed record SavedLocationResponse(
    Guid Id,
    string Title,
    decimal Latitude,
    decimal Longitude,
    string Address,
    string? City,
    string? District,
    bool IsDefault,
    DateTimeOffset CreatedAt);
