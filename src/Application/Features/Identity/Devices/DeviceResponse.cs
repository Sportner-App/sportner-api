namespace Sportner.Application.Features.Identity.Devices;

/// <summary>
/// Device projection for the owner. Never exposes the raw push token.
/// </summary>
public sealed record DeviceResponse(
    Guid Id,
    short Platform,
    string? DeviceName,
    string DeviceIdentifier,
    string? AppVersion,
    string? OsVersion,
    bool HasPushToken,
    DateTimeOffset? LastSeenAt,
    DateTimeOffset CreatedAt);
