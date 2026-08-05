using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Events;

public class EventParticipant : AuditableEntity
{
    private EventParticipant()
    {
    }

    public Guid EventId { get; private set; }

    public Guid UserId { get; private set; }

    public ParticipantStatus Status { get; private set; }

    public DateTimeOffset? JoinedAt { get; private set; }

    public DateTimeOffset? AttendedAt { get; private set; }

    public DateTimeOffset? LeftAt { get; private set; }

    public bool CanReview { get; private set; }

    public static EventParticipant CreateOrganizer(
        Guid eventId,
        Guid userId,
        DateTimeOffset utcNow)
    {
        EnsureIds(eventId, userId);

        return new EventParticipant
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            Status = ParticipantStatus.Approved,
            JoinedAt = utcNow,
            CanReview = false,
            CreatedAt = utcNow
        };
    }

    public static EventParticipant CreatePending(
        Guid eventId,
        Guid userId,
        DateTimeOffset utcNow)
    {
        EnsureIds(eventId, userId);

        return new EventParticipant
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            Status = ParticipantStatus.Pending,
            CanReview = false,
            CreatedAt = utcNow
        };
    }

    public static EventParticipant CreateApproved(
        Guid eventId,
        Guid userId,
        DateTimeOffset utcNow)
    {
        EnsureIds(eventId, userId);

        return new EventParticipant
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            Status = ParticipantStatus.Approved,
            JoinedAt = utcNow,
            CanReview = false,
            CreatedAt = utcNow
        };
    }

    public void Approve(DateTimeOffset utcNow)
    {
        if (Status is ParticipantStatus.Approved)
        {
            return;
        }

        if (Status is not ParticipantStatus.Pending)
        {
            throw new DomainException("Only pending participants can be approved.");
        }

        Status = ParticipantStatus.Approved;
        JoinedAt = utcNow;
        CanReview = false;
        Touch(utcNow);
    }

    public void Reject(DateTimeOffset utcNow)
    {
        if (Status is ParticipantStatus.Rejected)
        {
            return;
        }

        if (Status is not ParticipantStatus.Pending)
        {
            throw new DomainException("Only pending participants can be rejected.");
        }

        Status = ParticipantStatus.Rejected;
        CanReview = false;
        Touch(utcNow);
    }

    public void Cancel(DateTimeOffset utcNow)
    {
        if (Status is ParticipantStatus.Cancelled)
        {
            return;
        }

        if (Status is not (ParticipantStatus.Pending or ParticipantStatus.Approved))
        {
            throw new DomainException("Only pending or approved participants can cancel.");
        }

        Status = ParticipantStatus.Cancelled;
        LeftAt = utcNow;
        CanReview = false;
        Touch(utcNow);
    }

    public void ConfirmAttendance(DateTimeOffset utcNow)
    {
        if (Status is ParticipantStatus.Attended)
        {
            return;
        }

        if (Status is not ParticipantStatus.Approved)
        {
            throw new DomainException("Only approved participants can be marked as attended.");
        }

        Status = ParticipantStatus.Attended;
        AttendedAt = utcNow;
        CanReview = true;
        Touch(utcNow);
    }

    public void MarkNoShow(DateTimeOffset utcNow)
    {
        if (Status is ParticipantStatus.NoShow)
        {
            return;
        }

        if (Status is not ParticipantStatus.Approved)
        {
            throw new DomainException("Only approved participants can be marked as no-show.");
        }

        Status = ParticipantStatus.NoShow;
        CanReview = false;
        Touch(utcNow);
    }

    public bool OccupiesCapacity()
    {
        return Status is ParticipantStatus.Pending
            or ParticipantStatus.Approved
            or ParticipantStatus.Attended
            or ParticipantStatus.NoShow;
    }

    private void Touch(DateTimeOffset utcNow)
    {
        UpdatedAt = utcNow;
    }

    private static void EnsureIds(Guid eventId, Guid userId)
    {
        if (eventId == Guid.Empty)
        {
            throw new DomainException("Event id is required.");
        }

        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }
    }
}
