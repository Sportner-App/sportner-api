using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.UserProfiles.UpdateVisibility;

public sealed record UpdateVisibilityCommand(bool IsProfilePublic) : ICommand<MyProfileResponse>;
