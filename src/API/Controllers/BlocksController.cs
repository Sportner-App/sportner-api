using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sportner.API.Common;
using Sportner.Application.Features.Social.Blocks.BlockUser;
using Sportner.Application.Features.Social.Blocks.ListBlockedUsers;
using Sportner.Application.Features.Social.Blocks.UnblockUser;

namespace Sportner.API.Controllers;

[Authorize]
[Route("api/blocks")]
public sealed class BlocksController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new ListBlockedUsersQuery(page, pageSize), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> Block(
        [FromBody] BlockUserBody request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new BlockUserCommand(request.UserId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> Unblock(Guid userId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new UnblockUserCommand(userId), cancellationToken);
        return result.ToActionResult(StatusCodes.Status204NoContent);
    }

    public sealed record BlockUserBody(Guid UserId);
}
