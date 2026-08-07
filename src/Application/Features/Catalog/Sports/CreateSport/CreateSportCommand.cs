using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Catalog.Sports.CreateSport;

public sealed record CreateSportCommand(
    string Name,
    int DisplayOrder,
    string? Slug = null,
    string? IconUrl = null) : ICommand<SportResponse>;
