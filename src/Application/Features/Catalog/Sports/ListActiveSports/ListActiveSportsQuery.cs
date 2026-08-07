using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Catalog.Sports.ListActiveSports;

public sealed record ListActiveSportsQuery : IQuery<IReadOnlyList<SportResponse>>;
