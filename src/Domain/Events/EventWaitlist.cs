using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Events;

public class EventWaitlist : AuditableEntity
{
    private EventWaitlist()
    {
    }

    public Guid EventId { get; private set; }

    public Guid UserId { get; private set; }

    public int Position { get; private set; }

    public static EventWaitlist Create(
        Guid eventId,
        Guid userId,
        int position,
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

        return new EventWaitlist
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            Position = NormalizePosition(position),
            CreatedAt = utcNow
        };
    }

    public void ChangePosition(int position, DateTimeOffset utcNow)
    {
        var normalized = NormalizePosition(position);

        if (Position == normalized)
        {
            return;
        }

        Position = normalized;
        Touch(utcNow);
    }

    private void Touch(DateTimeOffset utcNow)
    {
        UpdatedAt = utcNow;
    }

    private static int NormalizePosition(int position)
    {
        if (position <= 0)
        {
            throw new DomainException("Waitlist position must be greater than zero.");
        }

        return position;
    }
}
