namespace Sportner.API.Authorization;

public sealed class ModeratorAuthorizationOptions
{
    public const string SectionName = "Authorization";

    /// <summary>
    /// Temporary allow-list of user ids that may use moderator endpoints.
    /// Replace with role claims before production scale.
    /// </summary>
    public List<Guid> ModeratorUserIds { get; set; } = [];
}
