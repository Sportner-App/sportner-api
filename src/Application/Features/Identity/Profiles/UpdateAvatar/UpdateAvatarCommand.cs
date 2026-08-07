using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.Profiles.UpdateAvatar;

/// <summary>
/// A null <paramref name="Content"/> removes the current avatar.
/// </summary>
public sealed record UpdateAvatarCommand(Stream? Content, string? ContentType, string? FileName)
    : ICommand<MyProfileResponse>;
