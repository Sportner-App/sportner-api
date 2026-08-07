using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.UserSports.RemoveSport;

public sealed record RemoveSportCommand(Guid SportId)
    : ICommand<IReadOnlyList<UserSportResponse>>;
