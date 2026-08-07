using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.SavedLocations.AddSavedLocation;

public sealed record AddSavedLocationCommand(
    string Title,
    decimal Latitude,
    decimal Longitude,
    string Address,
    string? City,
    string? District,
    bool IsDefault) : ICommand<SavedLocationResponse>;
