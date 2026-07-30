using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sportner.Application.DTOs.Auth;
using Sportner.Application.DTOs.Events;
using Sportner.Application.Services;

namespace Sportner.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController(IEventService eventService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<EventDto>>> GetEvents(
        [FromQuery] EventListQuery query,
        CancellationToken cancellationToken)
    {
        var result = await eventService.GetEventsAsync(query, cancellationToken);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("me/past")]
    [ProducesResponseType(typeof(IReadOnlyList<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<EventDto>>> GetMyPastEvents(
        CancellationToken cancellationToken)
    {
        var result = await eventService.GetMyPastEventsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDetailDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await eventService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EventDto>> Create(
        [FromBody] CreateEventDto dto,
        CancellationToken cancellationToken)
    {
        var result = await eventService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MessageResponseDto>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await eventService.DeleteAsync(id, cancellationToken);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("{id:guid}/join")]
    [ProducesResponseType(typeof(ParticipantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ParticipantDto>> Join(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await eventService.JoinAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/participants")]
    [ProducesResponseType(typeof(IReadOnlyList<ParticipantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ParticipantDto>>> GetParticipants(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await eventService.GetParticipantsAsync(id, cancellationToken);
        return Ok(result);
    }

    [Authorize]
    [HttpPatch("{id:guid}/participants/{userId:guid}")]
    [ProducesResponseType(typeof(ParticipantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ParticipantDto>> UpdateParticipantStatus(
        Guid id,
        Guid userId,
        [FromBody] UpdateParticipantStatusDto dto,
        CancellationToken cancellationToken)
    {
        var result = await eventService.UpdateParticipantStatusAsync(
            id,
            userId,
            dto,
            cancellationToken);
        return Ok(result);
    }
}
