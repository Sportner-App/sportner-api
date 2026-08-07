using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sportner.API.Common;
using Sportner.Application.Features.Moderation.ListActiveReportReasons;

namespace Sportner.API.Controllers;

[Authorize]
[Route("api/report-reasons")]
public sealed class ReportReasonsController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListActive(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ListActiveReportReasonsQuery(), cancellationToken);
        return result.ToActionResult();
    }
}
