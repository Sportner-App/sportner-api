using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.Profiles.UpdateVisibility;

public sealed record UpdateVisibilityCommand(bool IsProfilePublic) : ICommand<MyProfileResponse>;
