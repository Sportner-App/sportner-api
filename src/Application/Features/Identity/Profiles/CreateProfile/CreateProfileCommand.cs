using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.Profiles.CreateProfile;

public sealed record CreateProfileCommand(
    string Username,
    string FirstName,
    string? LastName,
    string? Bio,
    string? City,
    bool IsProfilePublic) : ICommand<MyProfileResponse>;
