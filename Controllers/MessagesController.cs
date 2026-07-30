using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportnerApi.Data;
using SportnerApi.Dtos;
using SportnerApi.Models;

namespace SportnerApi.Controllers;

[Authorize]
[ApiController]
[Route("api/Events/{eventId:guid}/messages")]
public class MessagesController(AppDbContext db) : ControllerBase
{
    private const string ApprovedStatus = "approved";

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<MessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessages(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var eventEntity = await db.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

        if (eventEntity is null)
        {
            return NotFound(new { message = "Etkinlik bulunamadı." });
        }

        if (!await CanAccessChatAsync(eventId, eventEntity.CreatedBy, userId.Value, cancellationToken))
        {
            return Forbid();
        }

        var messages = await db.Messages
            .AsNoTracking()
            .Include(m => m.User)
            .Where(m => m.EventId == eventId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(messages.Select(MapToDto));
    }

    [HttpPost]
    [ProducesResponseType(typeof(MessageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MessageDto>> CreateMessage(
        Guid eventId,
        [FromBody] CreateMessageDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var eventEntity = await db.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

        if (eventEntity is null)
        {
            return NotFound(new { message = "Etkinlik bulunamadı." });
        }

        if (!await CanAccessChatAsync(eventId, eventEntity.CreatedBy, userId.Value, cancellationToken))
        {
            return Forbid();
        }

        var content = dto.Content.Trim();
        if (string.IsNullOrWhiteSpace(content))
        {
            return BadRequest(new { message = "Mesaj içeriği boş olamaz." });
        }

        var message = new Message
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId.Value,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };

        db.Messages.Add(message);
        await db.SaveChangesAsync(cancellationToken);

        await db.Entry(message).Reference(m => m.User).LoadAsync(cancellationToken);

        return CreatedAtAction(nameof(GetMessages), new { eventId }, MapToDto(message));
    }

    private async Task<bool> CanAccessChatAsync(
        Guid eventId,
        Guid createdBy,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (createdBy == userId)
        {
            return true;
        }

        return await db.EventParticipants.AnyAsync(
            p => p.EventId == eventId &&
                 p.UserId == userId &&
                 p.Status == ApprovedStatus,
            cancellationToken);
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    private static MessageDto MapToDto(Message message) => new(
        message.Id,
        message.EventId,
        message.UserId,
        message.User?.FullName,
        message.User?.AvatarUrl,
        message.Content,
        message.CreatedAt
    );
}
