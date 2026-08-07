using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sportner.API.Common;
using Sportner.Application.Features.Identity.Sessions.ListMySessions;
using Sportner.Application.Features.Identity.Sessions.RevokeSession;

namespace Sportner.API.Controllers;

[Authorize]
[Route("api/me/sessions")]
public sealed class SessionsController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListMySessions(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ListMySessionsQuery(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{sessionId:guid}")]
    public async Task<IActionResult> RevokeSession(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RevokeSessionCommand(sessionId), cancellationToken);
        return result.ToActionResult(StatusCodes.Status204NoContent);
    }
}
