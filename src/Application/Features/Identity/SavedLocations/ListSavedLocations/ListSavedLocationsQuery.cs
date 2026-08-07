using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.SavedLocations.ListSavedLocations;

public sealed record ListSavedLocationsQuery : IQuery<IReadOnlyList<SavedLocationResponse>>;
