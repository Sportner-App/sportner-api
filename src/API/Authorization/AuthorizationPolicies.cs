namespace Sportner.API.Authorization;

public static class AuthorizationPolicies
{
    /// <summary>
    /// Authenticated only — used for logout so suspended users can still end sessions.
    /// </summary>
    public const string Authenticated = "Authenticated";

    public const string ActiveUser = "ActiveUser";

    public const string CanCreateContent = "CanCreateContent";

    public const string Moderator = "Moderator";

    public const string Admin = "Admin";
}
