using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.Profiles.UpdateUsername;

public sealed record UpdateUsernameCommand(string Username) : ICommand<MyProfileResponse>;
