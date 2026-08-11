using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sportner.API.Authorization;
using Sportner.API.Common;
using Sportner.API.Extensions.RateLimiting;
using Sportner.Application.Features.Moderation.CreateReport;
using Sportner.Application.Features.Moderation.GetMyReports;
using Sportner.Application.Features.Moderation.ListReports;
using Sportner.Application.Features.Moderation.RejectReport;
using Sportner.Application.Features.Moderation.ResolveReport;
using Sportner.Application.Features.Moderation.StartReportReview;
using Sportner.Application.Features.Moderation.UpdateReportDescription;

namespace Sportner.API.Controllers;

[Authorize]
[Route("api/reports")]
public sealed class ReportsController : ApiControllerBase
{
    [HttpPost]
    [EnableRateLimiting(RateLimitingExtensions.ReportPolicy)]
    public async Task<IActionResult> Create(
        [FromBody] CreateReportBody request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new CreateReportCommand(
                request.EntityType,
                request.EntityId,
                request.ReportReasonId,
                request.Description),
            cancellationToken);

        return result.ToActionResult(StatusCodes.Status201Created);
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new GetMyReportsQuery(page, pageSize), cancellationToken);
        return result.ToActionResult();
    }

    [Authorize(Policy = AuthorizationPolicies.Moderator)]
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] short? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
            new ListReportsQuery(status, page, pageSize),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPut("{reportId:guid}/description")]
    public async Task<IActionResult> UpdateDescription(
        Guid reportId,
        [FromBody] UpdateDescriptionBody request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new UpdateReportDescriptionCommand(reportId, request.Description),
            cancellationToken);

        return result.ToActionResult();
    }

    [Authorize(Policy = AuthorizationPolicies.Moderator)]
    [HttpPost("{reportId:guid}/start-review")]
    public async Task<IActionResult> StartReview(Guid reportId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new StartReportReviewCommand(reportId), cancellationToken);
        return result.ToActionResult();
    }

    [Authorize(Policy = AuthorizationPolicies.Moderator)]
    [HttpPost("{reportId:guid}/resolve")]
    public async Task<IActionResult> Resolve(
        Guid reportId,
        [FromBody] ResolutionBody request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new ResolveReportCommand(reportId, request.ResolutionNote),
            cancellationToken);

        return result.ToActionResult();
    }

    [Authorize(Policy = AuthorizationPolicies.Moderator)]
    [HttpPost("{reportId:guid}/reject")]
    public async Task<IActionResult> Reject(
        Guid reportId,
        [FromBody] ResolutionBody request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new RejectReportCommand(reportId, request.ResolutionNote),
            cancellationToken);

        return result.ToActionResult();
    }

    public sealed record CreateReportBody(
        short EntityType,
        Guid EntityId,
        Guid ReportReasonId,
        string? Description);

    public sealed record UpdateDescriptionBody(string? Description);

    public sealed record ResolutionBody(string ResolutionNote);
}
