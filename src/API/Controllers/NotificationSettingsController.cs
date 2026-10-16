using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sportner.API.Common;
using Sportner.Application.Features.Notifications.GetMyNotificationSettings;
using Sportner.Application.Features.Notifications.UpdateNotificationSetting;
using Sportner.Application.Features.Notifications.UpdateNotificationSettings;

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

    /// <summary>
    /// Ayarlar ekranı türleri gruplar halinde sunuyor; tek anahtar onlarca türü
    /// birden değiştirdiği için grup tek işlemde yazılır.
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> UpdateMany(
        [FromBody] UpdateNotificationSettingsBody request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new UpdateNotificationSettingsCommand(request.Settings),
            cancellationToken);

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

    public sealed record UpdateNotificationSettingsBody(
        IReadOnlyList<NotificationSettingUpdate> Settings);

    public sealed record UpdateNotificationSettingBody(
        bool InAppEnabled,
        bool PushEnabled,
        bool EmailEnabled);
}
