using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sportner.API.Authorization;
using Sportner.API.Common;
using Sportner.Application.Features.Catalog.Sports.ActivateSport;
using Sportner.Application.Features.Catalog.Sports.ChangeSportDisplayOrder;
using Sportner.Application.Features.Catalog.Sports.CreateSport;
using Sportner.Application.Features.Catalog.Sports.DeactivateSport;
using Sportner.Application.Features.Catalog.Sports.GetSportBySlug;
using Sportner.Application.Features.Catalog.Sports.ListActiveSports;
using Sportner.Application.Features.Catalog.Sports.RenameSport;
using Sportner.Application.Features.Catalog.Sports.UpdateSportCoverImage;

namespace Sportner.API.Controllers;

[Authorize]
public sealed class SportsController : ApiControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ListActiveSports(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
            new ListActiveSportsQuery(q, page, pageSize),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpGet("{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSportBySlug(string slug, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetSportBySlugQuery(slug), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    public async Task<IActionResult> Create(
        [FromBody] CreateSportRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new CreateSportCommand(
                request.Name,
                request.DisplayOrder,
                request.Slug,
                request.IconUrl),
            cancellationToken);

        return result.ToActionResult(StatusCodes.Status201Created);
    }

    [HttpPut("{sportId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    public async Task<IActionResult> Rename(
        Guid sportId,
        [FromBody] RenameSportRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new RenameSportCommand(sportId, request.Name, request.Slug, request.IconUrl),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPut("{sportId:guid}/display-order")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    public async Task<IActionResult> ChangeDisplayOrder(
        Guid sportId,
        [FromBody] ChangeDisplayOrderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new ChangeSportDisplayOrderCommand(sportId, request.DisplayOrder),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPut("{sportId:guid}/cover-image")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    public async Task<IActionResult> UpdateCoverImage(
        Guid sportId,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        await using var content = file?.OpenReadStream();

        var result = await Sender.Send(
            new UpdateSportCoverImageCommand(sportId, content, file?.ContentType, file?.FileName),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("{sportId:guid}/deactivate")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    public async Task<IActionResult> Deactivate(Guid sportId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new DeactivateSportCommand(sportId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{sportId:guid}/activate")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    public async Task<IActionResult> Activate(Guid sportId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ActivateSportCommand(sportId), cancellationToken);
        return result.ToActionResult();
    }

    public sealed record CreateSportRequest(
        string Name,
        int DisplayOrder,
        string? Slug = null,
        string? IconUrl = null);

    public sealed record RenameSportRequest(
        string Name,
        string? Slug = null,
        string? IconUrl = null);

    public sealed record ChangeDisplayOrderRequest(int DisplayOrder);
}
