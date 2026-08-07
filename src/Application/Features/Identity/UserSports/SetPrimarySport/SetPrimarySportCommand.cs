using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.UserSports.SetPrimarySport;

public sealed record SetPrimarySportCommand(Guid SportId)
    : ICommand<IReadOnlyList<UserSportResponse>>;
