using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportnerApi.Data;
using SportnerApi.Dtos;
using SportnerApi.Models;
using SportnerApi.Services;

namespace SportnerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController(AppDbContext db, INotificationService notificationService) : ControllerBase
{
    private const string PendingStatus = "pending";
    private const string ApprovedStatus = "approved";
    private const string RejectedStatus = "rejected";

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetEvents(
        [FromQuery] string? sportType = null,
        [FromQuery] string? search = null,
        [FromQuery] double? latitude = null,
        [FromQuery] double? longitude = null,
        [FromQuery] double? radiusKm = null,
        [FromQuery] string timeframe = "upcoming",
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var normalizedTimeframe = timeframe.Trim().ToLowerInvariant();

        if (normalizedTimeframe is not ("upcoming" or "past" or "all"))
        {
            return BadRequest(new { message = "timeframe yalnızca 'upcoming', 'past' veya 'all' olabilir." });
        }

        if (latitude.HasValue != longitude.HasValue)
        {
            return BadRequest(new { message = "latitude ve longitude birlikte gönderilmelidir." });
        }

        if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
        {
            return BadRequest(new { message = "Geçerli latitude ve longitude değerleri gönderilmelidir." });
        }

        if (radiusKm is <= 0)
        {
            return BadRequest(new { message = "radiusKm sıfırdan büyük olmalıdır." });
        }

        if (radiusKm is not null && latitude is null)
        {
            return BadRequest(new { message = "radiusKm filtresi için latitude ve longitude gereklidir." });
        }

        var query = db.Events
            .AsNoTracking()
            .Include(e => e.Organizer)
            .AsQueryable();

        query = normalizedTimeframe switch
        {
            "past" => query.Where(e => e.EventDate < now),
            "all" => query,
            _ => query.Where(e => e.EventDate >= now)
        };

        if (!string.IsNullOrWhiteSpace(sportType))
        {
            query = query.Where(e => e.SportType == sportType);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(e =>
                e.Title.ToLower().Contains(term) ||
                (e.Description != null && e.Description.ToLower().Contains(term)) ||
                (e.AddressText != null && e.AddressText.ToLower().Contains(term)));
        }

        if (latitude is not null && longitude is not null && radiusKm is not null)
        {
            var latDelta = radiusKm.Value / 111.0;
            var longitudeScale = Math.Max(
                Math.Abs(Math.Cos(DegreesToRadians(latitude.Value))),
                0.000001);
            var lonDelta = radiusKm.Value / (111.0 * longitudeScale);

            query = query.Where(e =>
                e.Latitude >= latitude.Value - latDelta &&
                e.Latitude <= latitude.Value + latDelta &&
                e.Longitude >= longitude.Value - lonDelta &&
                e.Longitude <= longitude.Value + lonDelta);
        }

        query = normalizedTimeframe == "past"
            ? query.OrderByDescending(e => e.EventDate)
            : query.OrderBy(e => e.EventDate);

        var events = await query.ToListAsync(cancellationToken);

        if (latitude is not null && longitude is not null)
        {
            var eventsWithDistance = events
                .Select(e => new
                {
                    Event = e,
                    DistanceKm = HaversineKm(
                        latitude.Value,
                        longitude.Value,
                        e.Latitude,
                        e.Longitude)
                });

            if (radiusKm is not null)
            {
                eventsWithDistance = eventsWithDistance
                    .Where(item => item.DistanceKm <= radiusKm.Value);
            }

            events = eventsWithDistance
                .OrderBy(item => item.DistanceKm)
                .ThenBy(item => item.Event.EventDate)
                .Select(item => item.Event)
                .ToList();
        }

        return Ok(events.Select(MapToEventDto));
    }

    /// <summary>
    /// Returns past events where the authenticated user is the organizer
    /// or an approved participant. Past = event_date &lt; UtcNow (derived, no status column).
    /// </summary>
    [Authorize]
    [HttpGet("me/past")]
    [ProducesResponseType(typeof(IEnumerable<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetMyPastEvents(
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var now = DateTime.UtcNow;

        var events = await db.Events
            .AsNoTracking()
            .Include(e => e.Organizer)
            .Where(e => e.EventDate < now)
            .Where(e =>
                e.CreatedBy == userId.Value ||
                e.Participants.Any(p => p.UserId == userId.Value && p.Status == ApprovedStatus))
            .OrderByDescending(e => e.EventDate)
            .ToListAsync(cancellationToken);

        return Ok(events.Select(MapToEventDto));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDetailDto>> GetEventById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var eventEntity = await db.Events
            .AsNoTracking()
            .Include(e => e.Organizer)
            .Include(e => e.Participants)
                .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (eventEntity is null)
        {
            return NotFound(new { message = "Etkinlik bulunamadı." });
        }

        return Ok(MapToDetailDto(eventEntity));
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EventDto>> CreateEvent(
        [FromBody] CreateEventDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId.Value,
            Title = dto.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            SportType = dto.SportType.Trim(),
            EventDate = dto.EventDate.ToUniversalTime(),
            MaxPlayers = dto.MaxPlayers,
            AddressText = string.IsNullOrWhiteSpace(dto.AddressText) ? null : dto.AddressText.Trim(),
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            ParticipantsCount = 0
        };

        db.Events.Add(eventEntity);
        await db.SaveChangesAsync(cancellationToken);

        await db.Entry(eventEntity).Reference(e => e.Organizer).LoadAsync(cancellationToken);

        return CreatedAtAction(nameof(GetEventById), new { id = eventEntity.Id }, MapToEventDto(eventEntity));
    }

    /// <summary>
    /// Deletes an event. Only the organizer (created_by) can delete.
    /// Related participants/messages/reviews are removed via cascade.
    /// </summary>
    [Authorize]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var eventEntity = await db.Events
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (eventEntity is null)
        {
            return NotFound(new { message = "Etkinlik bulunamadı." });
        }

        if (eventEntity.CreatedBy != userId.Value)
        {
            return Forbid();
        }

        db.Events.Remove(eventEntity);
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Etkinlik silindi." });
    }

    [Authorize]
    [HttpPost("{id:guid}/join")]
    [ProducesResponseType(typeof(ParticipantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ParticipantDto>> JoinEvent(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var eventEntity = await db.Events
            .Include(e => e.Participants)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (eventEntity is null)
        {
            return NotFound(new { message = "Etkinlik bulunamadı." });
        }

        if (eventEntity.CreatedBy == userId.Value)
        {
            return BadRequest(new { message = "Kendi etkinliğinize katılım isteği gönderemezsiniz." });
        }

        var existing = eventEntity.Participants.FirstOrDefault(p => p.UserId == userId.Value);
        if (existing is not null)
        {
            if (existing.Status is PendingStatus or ApprovedStatus)
            {
                return BadRequest(new { message = "Bu etkinlik için zaten bir katılım kaydınız var." });
            }

            existing.Status = PendingStatus;
        }
        else
        {
            existing = new EventParticipant
            {
                Id = Guid.NewGuid(),
                EventId = eventEntity.Id,
                UserId = userId.Value,
                Status = PendingStatus,
                CreatedAt = DateTime.UtcNow
            };
            db.EventParticipants.Add(existing);
        }

        await db.SaveChangesAsync(cancellationToken);

        var requester = await db.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == userId.Value, cancellationToken);

        var owner = await db.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == eventEntity.CreatedBy, cancellationToken);

        var requesterName = string.IsNullOrWhiteSpace(requester?.FullName)
            ? "Bir kullanıcı"
            : requester!.FullName!;

        await notificationService.SendPushNotificationAsync(
            owner?.PushToken,
            "Yeni Katılım İsteği",
            $"{requesterName} '{eventEntity.Title}' etkinliğinize katılmak istiyor.",
            new { type = "join_request", eventId = eventEntity.Id, userId = userId.Value },
            cancellationToken);

        return Ok(new ParticipantDto(
            existing.UserId,
            requester?.FullName,
            requester?.AvatarUrl,
            ResolveSkillLevel(requester?.SkillLevels, eventEntity.SportType),
            existing.Status
        ));
    }

    [HttpGet("{id:guid}/participants")]
    [ProducesResponseType(typeof(IEnumerable<ParticipantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ParticipantDto>>> GetParticipants(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var eventEntity = await db.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (eventEntity is null)
        {
            return NotFound(new { message = "Etkinlik bulunamadı." });
        }

        var participants = await db.EventParticipants
            .AsNoTracking()
            .Include(p => p.User)
            .Where(p => p.EventId == id)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(participants.Select(p => new ParticipantDto(
            p.UserId,
            p.User?.FullName,
            p.User?.AvatarUrl,
            ResolveSkillLevel(p.User?.SkillLevels, eventEntity.SportType),
            p.Status
        )));
    }

    [Authorize]
    [HttpPatch("{id:guid}/participants/{userId:guid}")]
    [ProducesResponseType(typeof(ParticipantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ParticipantDto>> UpdateParticipantStatus(
        Guid id,
        Guid userId,
        [FromBody] UpdateParticipantStatusDto dto,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        var status = dto.Status.Trim().ToLowerInvariant();
        if (status is not (ApprovedStatus or RejectedStatus))
        {
            return BadRequest(new { message = "Status yalnızca 'approved' veya 'rejected' olabilir." });
        }

        var eventEntity = await db.Events
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (eventEntity is null)
        {
            return NotFound(new { message = "Etkinlik bulunamadı." });
        }

        if (eventEntity.CreatedBy != currentUserId.Value)
        {
            return Forbid();
        }

        var participant = await db.EventParticipants
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.EventId == id && p.UserId == userId, cancellationToken);

        if (participant is null)
        {
            return NotFound(new { message = "Katılım isteği bulunamadı." });
        }

        var wasApproved = participant.Status == ApprovedStatus;

        if (status == ApprovedStatus)
        {
            var approvedCount = await db.EventParticipants
                .CountAsync(p => p.EventId == id && p.Status == ApprovedStatus, cancellationToken);

            if (!wasApproved && approvedCount >= eventEntity.MaxPlayers)
            {
                return BadRequest(new { message = "Etkinlik kontenjanı dolu." });
            }
        }

        participant.Status = status;

        if (status == ApprovedStatus && !wasApproved)
        {
            eventEntity.ParticipantsCount += 1;
        }
        else if (status == RejectedStatus && wasApproved)
        {
            eventEntity.ParticipantsCount = Math.Max(0, eventEntity.ParticipantsCount - 1);
        }

        await db.SaveChangesAsync(cancellationToken);

        if (status == ApprovedStatus)
        {
            await notificationService.SendPushNotificationAsync(
                participant.User?.PushToken,
                "İsteğin Onaylandı",
                $"'{eventEntity.Title}' etkinliğine katılımın onaylandı.",
                new { type = "join_approved", eventId = eventEntity.Id },
                cancellationToken);
        }
        else
        {
            await notificationService.SendPushNotificationAsync(
                participant.User?.PushToken,
                "İsteğin Reddedildi",
                $"'{eventEntity.Title}' etkinliğine katılımın reddedildi.",
                new { type = "join_rejected", eventId = eventEntity.Id },
                cancellationToken);
        }

        return Ok(new ParticipantDto(
            participant.UserId,
            participant.User?.FullName,
            participant.User?.AvatarUrl,
            ResolveSkillLevel(participant.User?.SkillLevels, eventEntity.SportType),
            participant.Status
        ));
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    private static EventDto MapToEventDto(Event e) => new(
        e.Id,
        e.Title,
        e.Description,
        e.SportType,
        e.EventDate,
        e.MaxPlayers,
        e.AddressText,
        e.Latitude,
        e.Longitude,
        e.ParticipantsCount,
        e.CreatedBy,
        e.Organizer?.FullName,
        e.Organizer?.AvatarUrl,
        e.CreatedAt
    );

    private static EventDetailDto MapToDetailDto(Event eventEntity)
    {
        var participants = eventEntity.Participants
            .Select(p => new ParticipantDto(
                p.UserId,
                p.User?.FullName,
                p.User?.AvatarUrl,
                ResolveSkillLevel(p.User?.SkillLevels, eventEntity.SportType),
                p.Status
            ))
            .ToList();

        return new EventDetailDto(
            eventEntity.Id,
            eventEntity.Title,
            eventEntity.Description,
            eventEntity.SportType,
            eventEntity.EventDate,
            eventEntity.MaxPlayers,
            eventEntity.AddressText,
            eventEntity.Latitude,
            eventEntity.Longitude,
            eventEntity.ParticipantsCount,
            eventEntity.CreatedBy,
            eventEntity.Organizer?.FullName,
            eventEntity.Organizer?.AvatarUrl,
            eventEntity.CreatedAt,
            participants.Count(p => p.Status == ApprovedStatus),
            participants
        );
    }

    private static string? ResolveSkillLevel(string? skillLevelsJson, string sportType)
    {
        if (string.IsNullOrWhiteSpace(skillLevelsJson))
        {
            return null;
        }

        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(skillLevelsJson);
            if (document.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object)
            {
                return null;
            }

            if (document.RootElement.TryGetProperty(sportType, out var exact))
            {
                return exact.ValueKind == System.Text.Json.JsonValueKind.String
                    ? exact.GetString()
                    : exact.ToString();
            }

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (string.Equals(property.Name, sportType, StringComparison.OrdinalIgnoreCase))
                {
                    return property.Value.ValueKind == System.Text.Json.JsonValueKind.String
                        ? property.Value.GetString()
                        : property.Value.ToString();
                }
            }
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }

        return null;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371.0;
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);
        var a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusKm * c;
    }
}
