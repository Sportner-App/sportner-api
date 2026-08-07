namespace Sportner.Application.Features.Identity.UserSports;

public sealed record UserSportResponse(
    Guid SportId,
    string SportName,
    string SportSlug,
    short SkillLevel,
    bool IsPrimary);
