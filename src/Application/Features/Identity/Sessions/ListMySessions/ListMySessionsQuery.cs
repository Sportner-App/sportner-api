using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.Sessions.ListMySessions;

public sealed record ListMySessionsQuery : IQuery<IReadOnlyList<SessionResponse>>;
