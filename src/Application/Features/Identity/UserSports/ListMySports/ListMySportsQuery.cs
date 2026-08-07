using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.UserSports.ListMySports;

public sealed record ListMySportsQuery : IQuery<IReadOnlyList<UserSportResponse>>;
