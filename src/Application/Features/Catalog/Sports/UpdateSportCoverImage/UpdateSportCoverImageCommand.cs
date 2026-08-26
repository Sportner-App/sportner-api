using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Catalog.Sports.UpdateSportCoverImage;

/// <summary>
/// A null <paramref name="Content"/> removes the current cover image.
/// </summary>
public sealed record UpdateSportCoverImageCommand(
    Guid SportId,
    Stream? Content,
    string? ContentType,
    string? FileName) : ICommand<SportResponse>;
