using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Identity.Devices.RemoveDevice;

public sealed record RemoveDeviceCommand(Guid DeviceId) : ICommand;
