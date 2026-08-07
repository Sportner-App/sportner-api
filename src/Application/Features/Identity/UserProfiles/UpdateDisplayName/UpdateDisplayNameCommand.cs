using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.UserProfiles.UpdateDisplayName;

public sealed record UpdateDisplayNameCommand(string FirstName, string? LastName)
    : ICommand<MyProfileResponse>;
