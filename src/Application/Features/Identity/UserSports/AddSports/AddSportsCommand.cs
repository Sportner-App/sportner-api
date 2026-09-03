using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.UserSports.AddSports;

public sealed record AddSportsItem(Guid SportId, short SkillLevel, bool IsPrimary = false);

public sealed record AddSportsCommand(IReadOnlyList<AddSportsItem> Sports)
    : ICommand<IReadOnlyList<UserSportResponse>>;
