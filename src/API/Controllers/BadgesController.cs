using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sportner.API.Common;
using Sportner.Application.Features.Gamification.ListBadges;
using Sportner.Application.Features.Gamification.ListMyBadges;
using Sportner.Application.Features.Gamification.ListUserBadges;

namespace Sportner.API.Controllers;

[Authorize]
[Route("api/badges")]
public sealed class BadgesController : ApiControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ListBadgesQuery(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("me")]
    public async Task<IActionResult> ListMine(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ListMyBadgesQuery(), cancellationToken);
        return result.ToActionResult();
    }
}

[Authorize]
[Route("api/users")]
public sealed class UserBadgesController : ApiControllerBase
{
    [AllowAnonymous]
    [HttpGet("{userId:guid}/badges")]
    public async Task<IActionResult> ListForUser(Guid userId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ListUserBadgesQuery(userId), cancellationToken);
        return result.ToActionResult();
    }
}
