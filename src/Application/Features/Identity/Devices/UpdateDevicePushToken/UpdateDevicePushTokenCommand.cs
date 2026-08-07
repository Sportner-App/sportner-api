using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.Devices.UpdateDevicePushToken;

/// <summary>
/// A null <paramref name="PushToken"/> clears the current push token.
/// </summary>
public sealed record UpdateDevicePushTokenCommand(Guid DeviceId, string? PushToken)
    : ICommand<DeviceResponse>;
