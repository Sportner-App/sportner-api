using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sportner.API.Common;
using Sportner.Application.Features.Identity.UserSports.AddSport;
using Sportner.Application.Features.Identity.UserSports.AddSports;
using Sportner.Application.Features.Identity.UserSports.ChangeSportSkillLevel;
using Sportner.Application.Features.Identity.UserSports.ListMySports;
using Sportner.Application.Features.Identity.UserSports.RemoveSport;
using Sportner.Application.Features.Identity.UserSports.SetPrimarySport;

namespace Sportner.API.Controllers;

[Authorize]
[Route("api/me/sports")]
public sealed class UserSportsController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListMySports(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ListMySportsQuery(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> AddSport(
        [FromBody] AddSportRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new AddSportCommand(request.SportId, request.SkillLevel, request.IsPrimary),
            cancellationToken);

        return result.ToActionResult(StatusCodes.Status201Created);
    }

    [HttpPost("batch")]
    public async Task<IActionResult> AddSports(
        [FromBody] AddSportsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new AddSportsCommand(request.Sports
                .Select(item => new AddSportsItem(item.SportId, item.SkillLevel, item.IsPrimary))
                .ToArray()),
            cancellationToken);

        return result.ToActionResult(StatusCodes.Status201Created);
    }

    [HttpPut("{sportId:guid}")]
    public async Task<IActionResult> ChangeSkillLevel(
        Guid sportId,
        [FromBody] ChangeSkillLevelRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new ChangeSportSkillLevelCommand(sportId, request.SkillLevel),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPut("{sportId:guid}/primary")]
    public async Task<IActionResult> SetPrimary(Guid sportId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new SetPrimarySportCommand(sportId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{sportId:guid}")]
    public async Task<IActionResult> RemoveSport(Guid sportId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RemoveSportCommand(sportId), cancellationToken);
        return result.ToActionResult();
    }

    public sealed record AddSportRequest(Guid SportId, short SkillLevel, bool IsPrimary = false);

    public sealed record AddSportsRequest(IReadOnlyList<AddSportsItemRequest> Sports);

    public sealed record AddSportsItemRequest(Guid SportId, short SkillLevel, bool IsPrimary = false);

    public sealed record ChangeSkillLevelRequest(short SkillLevel);
}
