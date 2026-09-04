namespace Sportner.Application.Abstractions.Authentication;

public sealed record ExternalRegistrationToken(string Token, DateTimeOffset ExpiresAt);
