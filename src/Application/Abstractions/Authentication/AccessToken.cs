namespace Sportner.Application.Abstractions.Authentication;

/// <summary>
/// A freshly issued JWT access token together with its absolute expiry.
/// </summary>
public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);
