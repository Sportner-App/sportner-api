namespace Sportner.Application.Features.Identity.Auth;

public sealed record AuthenticationResponse(
    Guid UserId,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    bool IsNewUser,
    bool IsOnboardingCompleted);
