using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.UserSports.ChangeSportSkillLevel;

public sealed record ChangeSportSkillLevelCommand(Guid SportId, short SkillLevel)
    : ICommand<IReadOnlyList<UserSportResponse>>;
