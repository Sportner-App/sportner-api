using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sportner.API.Common;
using Sportner.Application.Features.Quests.ListMyQuests;
using Sportner.Application.Features.Quests.ListQuests;

namespace Sportner.API.Controllers;

[Authorize]
[Route("api/quests")]
public sealed class QuestsController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ListQuestsQuery(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("me")]
    public async Task<IActionResult> ListMine(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ListMyQuestsQuery(), cancellationToken);
        return result.ToActionResult();
    }
}
