using Sportner.Domain.Users;

namespace Sportner.Application.Features.Identity.Devices;

internal static class DeviceMapping
{
    internal static DeviceResponse ToResponse(this UserDevice device) =>
        new(
            device.Id,
            (short)device.Platform,
            device.DeviceName,
            device.DeviceIdentifier,
            device.AppVersion,
            device.OsVersion,
            !string.IsNullOrEmpty(device.PushToken),
            device.LastSeenAt,
            device.CreatedAt);
}
