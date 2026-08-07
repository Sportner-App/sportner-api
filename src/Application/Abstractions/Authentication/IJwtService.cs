using System.Security.Claims;

namespace Sportner.Application.Abstractions.Authentication;

/// <summary>
/// Issues JWT access tokens and opaque refresh tokens for authenticated users.
/// Refresh tokens are returned in plaintext only once; only their hash is persisted
/// on <c>UserSessions</c>.
/// </summary>
public interface IJwtService
{
    AccessToken CreateAccessToken(Guid userId, IEnumerable<Claim>? additionalClaims = null);

    /// <summary>
    /// Generates a cryptographically strong opaque refresh token (plaintext) with its expiry.
    /// </summary>
    RefreshToken GenerateRefreshToken();
}
