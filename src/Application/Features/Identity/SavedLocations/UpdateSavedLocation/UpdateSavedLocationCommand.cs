using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.SavedLocations.UpdateSavedLocation;

public sealed record UpdateSavedLocationCommand(
    Guid LocationId,
    string Title,
    decimal Latitude,
    decimal Longitude,
    string Address,
    string? City,
    string? District) : ICommand<SavedLocationResponse>;
