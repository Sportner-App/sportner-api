using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sportner.API.Common;
using Sportner.Application.Features.Leaderboards;

namespace Sportner.API.Controllers;

[Authorize]
[Route("api/leaderboards")]
public sealed class LeaderboardsController : ApiControllerBase
{
    [HttpGet("districts")]
    public async Task<IActionResult> ListDistricts(
        [FromQuery] string? city,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ListDistrictsQuery(city), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("district")]
    public async Task<IActionResult> GetDistrict(
        [FromQuery] string city,
        [FromQuery] string district,
        [FromQuery] Guid? sportId,
        [FromQuery] LeaderboardPeriod period,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new GetDistrictLeaderboardQuery(city, district, sportId, period),
            cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyStanding(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetMyDistrictStandingQuery(), cancellationToken);
        return result.ToActionResult();
    }

    [AllowAnonymous]
    [HttpGet("crowns/{userId:guid}")]
    public async Task<IActionResult> ListCrowns(Guid userId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ListDistrictCrownsQuery(userId), cancellationToken);
        return result.ToActionResult();
    }
}
