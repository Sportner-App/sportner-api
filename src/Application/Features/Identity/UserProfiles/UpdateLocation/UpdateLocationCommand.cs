using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.UserProfiles.UpdateLocation;

public sealed record UpdateLocationCommand(string? City) : ICommand<MyProfileResponse>;
