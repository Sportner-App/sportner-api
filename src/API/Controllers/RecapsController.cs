using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sportner.API.Common;
using Sportner.Application.Features.Recaps;

namespace Sportner.API.Controllers;

[Authorize]
[Route("api/recaps")]
public sealed class RecapsController : ApiControllerBase
{
    /// <summary>Kendi aylık özetin; <paramref name="month"/> (YYYY-MM) yoksa geçen ay.</summary>
    [HttpGet("monthly")]
    public async Task<IActionResult> GetMonthly(
        [FromQuery] string? month,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetMonthlyRecapQuery(month), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Kendi yıllık özetin (Wrapped); <paramref name="year"/> yoksa 15 Aralık'tan itibaren bu yıl.</summary>
    [HttpGet("yearly")]
    public async Task<IActionResult> GetYearly(
        [FromQuery] int? year,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetYearlyRecapQuery(year), cancellationToken);
        return result.ToActionResult();
    }
}
