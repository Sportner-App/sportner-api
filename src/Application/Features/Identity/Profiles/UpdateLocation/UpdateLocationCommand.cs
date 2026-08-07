using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.Profiles.UpdateLocation;

public sealed record UpdateLocationCommand(string? City) : ICommand<MyProfileResponse>;
