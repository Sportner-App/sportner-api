using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sportner.API.Common;
using Sportner.Application.Features.Gamification;
using Sportner.Application.Features.Gamification.GetMyBadgeProgress;
using Sportner.Application.Features.Gamification.ListBadges;
using Sportner.Application.Features.Gamification.ListMyBadges;
using Sportner.Application.Features.Gamification.ListUserBadges;
using Sportner.Application.Features.Gamification.SetShowcasedBadges;

namespace Sportner.API.Controllers;

[Authorize]
[Route("api/badges")]
public sealed class BadgesController : ApiControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] short? category,
        [FromQuery] bool? earned,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ListBadgesQuery(category, earned), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("me")]
    public async Task<IActionResult> ListMine(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ListMyBadgesQuery(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("me/progress")]
    public async Task<IActionResult> MyProgress(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetMyBadgeProgressQuery(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("me/showcase")]
    public async Task<IActionResult> SetShowcase(
        [FromBody] SetShowcasedBadgesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new SetShowcasedBadgesCommand(request.BadgeIds),
            cancellationToken);
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
