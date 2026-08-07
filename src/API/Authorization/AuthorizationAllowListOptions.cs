namespace Sportner.API.Authorization;

/// <summary>
/// Bound from the <c>Authorization</c> configuration section.
/// Allow-lists are temporary until role claims exist.
/// </summary>
public sealed class AuthorizationAllowListOptions
{
    public const string SectionName = "Authorization";

    public List<Guid> ModeratorUserIds { get; set; } = [];

    public List<Guid> AdminUserIds { get; set; } = [];
}
