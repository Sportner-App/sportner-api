namespace Sportner.Application.Features.Identity.Sessions;

/// <summary>
/// Session metadata for the owner. Never exposes the refresh token hash.
/// </summary>
public sealed record SessionResponse(
    Guid Id,
    Guid? DeviceId,
    string? IpAddress,
    string? UserAgent,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt);
