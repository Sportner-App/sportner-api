using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.Devices.RegisterDevice;

/// <summary>
/// Upsert by <paramref name="DeviceIdentifier"/>: registers a new device or refreshes the
/// existing one for the current user.
/// </summary>
public sealed record RegisterDeviceCommand(
    short Platform,
    string DeviceIdentifier,
    string? DeviceName,
    string? AppVersion,
    string? OsVersion,
    string? PushToken) : ICommand<DeviceResponse>;
