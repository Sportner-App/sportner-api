using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.SavedLocations.SetDefaultSavedLocation;

public sealed record SetDefaultSavedLocationCommand(Guid LocationId)
    : ICommand<IReadOnlyList<SavedLocationResponse>>;
