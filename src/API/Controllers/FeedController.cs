using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sportner.API.Common;
using Sportner.Application.Features.Social.Feed.GetExploreFeed;
using Sportner.Application.Features.Social.Feed.GetHomeFeed;

namespace Sportner.API.Controllers;

[Authorize]
[Route("api/feed")]
public sealed class FeedController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Home(
        [FromQuery] string? before,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new GetHomeFeedQuery(before, limit), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("explore")]
    public async Task<IActionResult> Explore(
        [FromQuery] string? before,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new GetExploreFeedQuery(before, limit), cancellationToken);
        return result.ToActionResult();
    }
}
