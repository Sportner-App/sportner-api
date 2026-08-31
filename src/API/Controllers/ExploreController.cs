using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sportner.API.Common;
using Sportner.Application.Features.Explore.ExploreEvents;
using Sportner.Application.Features.Explore.ExplorePeople;
using Sportner.Application.Features.Explore.ExplorePosts;

namespace Sportner.API.Controllers;

[Authorize]
[Route("api/explore")]
public sealed class ExploreController : ApiControllerBase
{
    [HttpGet("people")]
    public async Task<IActionResult> People(
        [FromQuery] Guid? sportId,
        [FromQuery] string? city,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
            new ExplorePeopleQuery(sportId, city, limit),
            cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("events")]
    public async Task<IActionResult> Events(
        [FromQuery] Guid? sportId,
        [FromQuery] string? city,
        [FromQuery] decimal? lat,
        [FromQuery] decimal? lng,
        [FromQuery] double? radiusKm,
        [FromQuery] short? skillLevel,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
            new ExploreEventsQuery(sportId, city, lat, lng, radiusKm, skillLevel, limit),
            cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("posts")]
    [AllowAnonymous]
    public async Task<IActionResult> Posts(
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new ExplorePostsQuery(limit), cancellationToken);
        return result.ToActionResult();
    }
}
