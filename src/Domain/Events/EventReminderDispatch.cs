using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Events;

/// <summary>
/// Idempotency record for event reminder windows (e.g. 24h / 1h before start).
/// </summary>
public class EventReminderDispatch : AuditableEntity
{
    private EventReminderDispatch()
    {
    }

    public Guid EventId { get; private set; }

    public Guid UserId { get; private set; }

    /// <summary>
    /// Minutes before <c>Event.EventDate</c> (1440 = 24h, 60 = 1h).
    /// </summary>
    public int WindowMinutes { get; private set; }

    public DateTimeOffset SentAt { get; private set; }

    public static EventReminderDispatch Create(
        Guid eventId,
        Guid userId,
        int windowMinutes,
        DateTimeOffset utcNow)
    {
        if (eventId == Guid.Empty)
        {
            throw new DomainException("Event id is required.");
        }

        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        if (windowMinutes <= 0)
        {
            throw new DomainException("Window minutes must be greater than zero.");
        }

        return new EventReminderDispatch
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            WindowMinutes = windowMinutes,
            SentAt = utcNow,
            CreatedAt = utcNow
        };
    }
}
