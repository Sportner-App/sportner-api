using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sportner.API.Common;
using Sportner.Application.Features.Catalog.Cities.ListCities;

namespace Sportner.API.Controllers;

[Authorize]
public sealed class CitiesController : ApiControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ListCitiesQuery(), cancellationToken);
        return result.ToActionResult();
    }
}
