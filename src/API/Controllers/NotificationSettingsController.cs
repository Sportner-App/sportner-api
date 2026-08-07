using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sportner.API.Common;
using Sportner.Application.Features.Notifications.GetMyNotificationSettings;
using Sportner.Application.Features.Notifications.UpdateNotificationSetting;

namespace Sportner.API.Controllers;

[Authorize]
[Route("api/notification-settings")]
public sealed class NotificationSettingsController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetMyNotificationSettingsQuery(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("{type:int}")]
    public async Task<IActionResult> Update(
        short type,
        [FromBody] UpdateNotificationSettingBody request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new UpdateNotificationSettingCommand(
                type,
                request.InAppEnabled,
                request.PushEnabled,
                request.EmailEnabled),
            cancellationToken);

        return result.ToActionResult();
    }

    public sealed record UpdateNotificationSettingBody(
        bool InAppEnabled,
        bool PushEnabled,
        bool EmailEnabled);
}
