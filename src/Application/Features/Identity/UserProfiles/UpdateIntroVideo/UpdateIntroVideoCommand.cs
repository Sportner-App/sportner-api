using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.UserProfiles.UpdateIntroVideo;

/// <summary>
/// A null <paramref name="Content"/> removes the current intro video.
/// </summary>
public sealed record UpdateIntroVideoCommand(Stream? Content, string? ContentType, string? FileName)
    : ICommand<MyProfileResponse>;
