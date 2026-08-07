using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sportner.API.Common;
using Sportner.Application.Features.Catalog.Sports.GetSportBySlug;
using Sportner.Application.Features.Catalog.Sports.ListActiveSports;

namespace Sportner.API.Controllers;

[Authorize]
public sealed class SportsController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListActiveSports(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ListActiveSportsQuery(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetSportBySlug(string slug, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetSportBySlugQuery(slug), cancellationToken);
        return result.ToActionResult();
    }
}
