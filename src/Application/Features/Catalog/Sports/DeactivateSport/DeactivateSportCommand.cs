using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Catalog.Sports.DeactivateSport;

public sealed record DeactivateSportCommand(Guid SportId) : ICommand<SportResponse>;
