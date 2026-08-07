using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Catalog.Sports.ChangeSportDisplayOrder;

public sealed record ChangeSportDisplayOrderCommand(
    Guid SportId,
    int DisplayOrder) : ICommand<SportResponse>;
