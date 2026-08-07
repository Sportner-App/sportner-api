namespace Sportner.Application.Abstractions.Authentication;

/// <summary>
/// A freshly generated opaque refresh token (plaintext) together with its absolute expiry.
/// Only the hash of <see cref="Token"/> is ever persisted.
/// </summary>
public sealed record RefreshToken(string Token, DateTimeOffset ExpiresAt);
