using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sportner.API.Common;
using Sportner.Application.Features.Identity.SavedLocations.AddSavedLocation;
using Sportner.Application.Features.Identity.SavedLocations.ListSavedLocations;
using Sportner.Application.Features.Identity.SavedLocations.RemoveSavedLocation;
using Sportner.Application.Features.Identity.SavedLocations.SetDefaultSavedLocation;
using Sportner.Application.Features.Identity.SavedLocations.UpdateSavedLocation;

namespace Sportner.API.Controllers;

[Authorize]
[Route("api/me/saved-locations")]
public sealed class SavedLocationsController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListSavedLocations(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ListSavedLocationsQuery(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> AddSavedLocation(
        [FromBody] AddSavedLocationRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddSavedLocationCommand(
            request.Title,
            request.Latitude,
            request.Longitude,
            request.Address,
            request.City,
            request.District,
            request.IsDefault);

        var result = await Sender.Send(command, cancellationToken);
        return result.ToActionResult(StatusCodes.Status201Created);
    }

    [HttpPut("{locationId:guid}")]
    public async Task<IActionResult> UpdateSavedLocation(
        Guid locationId,
        [FromBody] UpdateSavedLocationRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateSavedLocationCommand(
            locationId,
            request.Title,
            request.Latitude,
            request.Longitude,
            request.Address,
            request.City,
            request.District);

        var result = await Sender.Send(command, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("{locationId:guid}/default")]
    public async Task<IActionResult> SetDefault(Guid locationId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new SetDefaultSavedLocationCommand(locationId),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpDelete("{locationId:guid}")]
    public async Task<IActionResult> RemoveSavedLocation(
        Guid locationId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RemoveSavedLocationCommand(locationId), cancellationToken);
        return result.ToActionResult(StatusCodes.Status204NoContent);
    }

    public sealed record AddSavedLocationRequest(
        string Title,
        decimal Latitude,
        decimal Longitude,
        string Address,
        string? City,
        string? District,
        bool IsDefault = false);

    public sealed record UpdateSavedLocationRequest(
        string Title,
        decimal Latitude,
        decimal Longitude,
        string Address,
        string? City,
        string? District);
}
