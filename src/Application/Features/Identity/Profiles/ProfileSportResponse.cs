namespace Sportner.Application.Features.Identity.Profiles;

public sealed record ProfileSportResponse(
    Guid SportId,
    string SportName,
    string SportSlug,
    short SkillLevel,
    bool IsPrimary);
