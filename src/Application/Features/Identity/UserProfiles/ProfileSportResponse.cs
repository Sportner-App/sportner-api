namespace Sportner.Application.Features.Identity.UserProfiles;

public sealed record ProfileSportResponse(
    Guid SportId,
    string SportName,
    string SportSlug,
    short SkillLevel,
    bool IsPrimary);
