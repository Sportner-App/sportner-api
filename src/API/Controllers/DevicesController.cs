using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sportner.API.Common;
using Sportner.Application.Features.Identity.Devices.ListMyDevices;
using Sportner.Application.Features.Identity.Devices.RegisterDevice;
using Sportner.Application.Features.Identity.Devices.RemoveDevice;
using Sportner.Application.Features.Identity.Devices.UpdateDevicePushToken;

namespace Sportner.API.Controllers;

[Authorize]
[Route("api/me/devices")]
public sealed class DevicesController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListMyDevices(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ListMyDevicesQuery(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> RegisterDevice(
        [FromBody] RegisterDeviceRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterDeviceCommand(
            request.Platform,
            request.DeviceIdentifier,
            request.DeviceName,
            request.AppVersion,
            request.OsVersion,
            request.PushToken);

        var result = await Sender.Send(command, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("{deviceId:guid}/push-token")]
    public async Task<IActionResult> UpdatePushToken(
        Guid deviceId,
        [FromBody] UpdatePushTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new UpdateDevicePushTokenCommand(deviceId, request.PushToken),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpDelete("{deviceId:guid}")]
    public async Task<IActionResult> RemoveDevice(Guid deviceId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RemoveDeviceCommand(deviceId), cancellationToken);
        return result.ToActionResult(StatusCodes.Status204NoContent);
    }

    public sealed record RegisterDeviceRequest(
        short Platform,
        string DeviceIdentifier,
        string? DeviceName,
        string? AppVersion,
        string? OsVersion,
        string? PushToken);

    public sealed record UpdatePushTokenRequest(string? PushToken);
}
