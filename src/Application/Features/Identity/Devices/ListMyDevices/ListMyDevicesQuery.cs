using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.Devices.ListMyDevices;

public sealed record ListMyDevicesQuery : IQuery<IReadOnlyList<DeviceResponse>>;
