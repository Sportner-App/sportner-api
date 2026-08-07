using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sportner.API.Common;
using Sportner.Application.Features.Notifications.DeleteNotification;
using Sportner.Application.Features.Notifications.ListMyNotifications;
using Sportner.Application.Features.Notifications.MarkAllNotificationsRead;
using Sportner.Application.Features.Notifications.MarkNotificationRead;
using Sportner.Application.Features.Notifications.MarkNotificationUnread;

namespace Sportner.API.Controllers;

[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] bool unreadOnly = false,
        [FromQuery] string? before = null,
        [FromQuery] int limit = 30,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
            new ListMyNotificationsQuery(unreadOnly, before, limit),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new MarkAllNotificationsReadCommand(), cancellationToken);
        return result.ToActionResult(StatusCodes.Status204NoContent);
    }

    [HttpPost("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid notificationId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new MarkNotificationReadCommand(notificationId),
            cancellationToken);

        return result.ToActionResult(StatusCodes.Status204NoContent);
    }

    [HttpPost("{notificationId:guid}/unread")]
    public async Task<IActionResult> MarkUnread(Guid notificationId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new MarkNotificationUnreadCommand(notificationId),
            cancellationToken);

        return result.ToActionResult(StatusCodes.Status204NoContent);
    }

    [HttpDelete("{notificationId:guid}")]
    public async Task<IActionResult> Delete(Guid notificationId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new DeleteNotificationCommand(notificationId),
            cancellationToken);

        return result.ToActionResult(StatusCodes.Status204NoContent);
    }
}
