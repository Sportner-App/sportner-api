using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Catalog.Sports.RenameSport;

public sealed record RenameSportCommand(
    Guid SportId,
    string Name,
    string? Slug = null,
    string? IconUrl = null) : ICommand<SportResponse>;
