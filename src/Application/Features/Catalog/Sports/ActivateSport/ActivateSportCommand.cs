using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Catalog.Sports.ActivateSport;

public sealed record ActivateSportCommand(Guid SportId) : ICommand<SportResponse>;
