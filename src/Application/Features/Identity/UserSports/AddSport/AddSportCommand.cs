using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.UserSports.AddSport;

public sealed record AddSportCommand(Guid SportId, short SkillLevel, bool IsPrimary)
    : ICommand<IReadOnlyList<UserSportResponse>>;
