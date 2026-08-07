using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.SavedLocations.RemoveSavedLocation;

public sealed record RemoveSavedLocationCommand(Guid LocationId) : ICommand;
