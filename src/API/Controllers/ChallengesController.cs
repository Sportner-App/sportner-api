using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sportner.API.Common;
using Sportner.Application.Features.Challenges;

namespace Sportner.API.Controllers;

[Authorize]
[Route("api/challenges")]
public sealed class ChallengesController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ListChallengesQuery(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{challengeId:guid}/join")]
    public async Task<IActionResult> Join(Guid challengeId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new JoinChallengeCommand(challengeId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{challengeId:guid}/join")]
    public async Task<IActionResult> Leave(Guid challengeId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new LeaveChallengeCommand(challengeId), cancellationToken);
        return result.ToActionResult();
    }

    [AllowAnonymous]
    [HttpGet("medals/{userId:guid}")]
    public async Task<IActionResult> ListMedals(Guid userId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ListChallengeMedalsQuery(userId), cancellationToken);
        return result.ToActionResult();
    }
}
