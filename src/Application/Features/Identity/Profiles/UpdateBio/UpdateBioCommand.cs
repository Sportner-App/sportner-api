using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.Profiles.UpdateBio;

public sealed record UpdateBioCommand(string? Bio) : ICommand<MyProfileResponse>;
