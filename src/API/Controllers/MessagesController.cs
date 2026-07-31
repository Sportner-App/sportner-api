using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sportner.Application.DTOs.Messages;
using Sportner.Application.Services;

namespace Sportner.API.Controllers;

[Authorize]
[ApiController]
[Route("api/events/{eventId:guid}/messages")]
public class MessagesController(IMessageService messageService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<MessageDto>>> GetMessages(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var result = await messageService.GetMessagesAsync(eventId, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(MessageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MessageDto>> CreateMessage(
        Guid eventId,
        [FromBody] CreateMessageDto dto,
        CancellationToken cancellationToken)
    {
        var result = await messageService.CreateMessageAsync(eventId, dto, cancellationToken);
        return CreatedAtAction(nameof(GetMessages), new { eventId }, result);
    }
}
