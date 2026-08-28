using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Events;

public class EventParticipant : AuditableEntity
{
    public const int GuestNameMaxLength = 50;

    private EventParticipant()
    {
    }

    public Guid EventId { get; private set; }

    public Guid? UserId { get; private set; }

    public ParticipantKind Kind { get; private set; }

    public string? GuestFirstName { get; private set; }

    public string? GuestLastName { get; private set; }

    public ParticipantStatus Status { get; private set; }

    public DateTimeOffset? JoinedAt { get; private set; }

    public DateTimeOffset? AttendedAt { get; private set; }

    public DateTimeOffset? LeftAt { get; private set; }

    public bool CanReview { get; private set; }

    public bool IsGuest => Kind is ParticipantKind.Guest;

    public bool IsRegistered => Kind is ParticipantKind.Registered && UserId is not null;

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
            Kind = ParticipantKind.Registered,
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
            Kind = ParticipantKind.Registered,
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
            Kind = ParticipantKind.Registered,
            Status = ParticipantStatus.Approved,
            JoinedAt = utcNow,
            CanReview = false,
            CreatedAt = utcNow
        };
    }

    public static EventParticipant CreateInvited(
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
            Kind = ParticipantKind.Registered,
            Status = ParticipantStatus.Invited,
            CanReview = false,
            CreatedAt = utcNow
        };
    }

    public static EventParticipant CreateGuest(
        Guid eventId,
        DateTimeOffset utcNow,
        string? firstName = null,
        string? lastName = null)
    {
        if (eventId == Guid.Empty)
        {
            throw new DomainException("Event id is required.");
        }

        return new EventParticipant
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = null,
            Kind = ParticipantKind.Guest,
            GuestFirstName = NormalizeRequiredGuestName(firstName, "first"),
            GuestLastName = NormalizeRequiredGuestName(lastName, "last"),
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

        if (IsGuest)
        {
            throw new DomainException("Guest participants cannot be rejected.");
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

        if (Status is not (ParticipantStatus.Pending
            or ParticipantStatus.Approved
            or ParticipantStatus.Invited))
        {
            throw new DomainException("Only pending, invited or approved participants can cancel.");
        }

        Status = ParticipantStatus.Cancelled;
        LeftAt = utcNow;
        CanReview = false;
        Touch(utcNow);
    }

    public void ReopenAsPending(DateTimeOffset utcNow)
    {
        if (IsGuest)
        {
            throw new DomainException("Guest participants cannot re-apply.");
        }

        if (Status is not ParticipantStatus.Cancelled)
        {
            throw new DomainException("Only cancelled participants can re-apply.");
        }

        Status = ParticipantStatus.Pending;
        JoinedAt = null;
        LeftAt = null;
        AttendedAt = null;
        CanReview = false;
        Touch(utcNow);
    }

    public void ReopenAsApproved(DateTimeOffset utcNow)
    {
        if (IsGuest)
        {
            throw new DomainException("Guest participants cannot be restored as registered users.");
        }

        if (Status is not ParticipantStatus.Cancelled)
        {
            throw new DomainException("Only cancelled participants can be restored.");
        }

        Status = ParticipantStatus.Approved;
        JoinedAt = utcNow;
        LeftAt = null;
        AttendedAt = null;
        CanReview = false;
        Touch(utcNow);
    }

    public void ReopenAsInvited(DateTimeOffset utcNow)
    {
        if (IsGuest || Status is not ParticipantStatus.Cancelled)
        {
            throw new DomainException("Only cancelled registered participants can be invited again.");
        }

        Status = ParticipantStatus.Invited;
        JoinedAt = null;
        LeftAt = null;
        AttendedAt = null;
        CanReview = false;
        Touch(utcNow);
    }

    public void AcceptInvitation(DateTimeOffset utcNow)
    {
        if (Status is not ParticipantStatus.Invited)
        {
            throw new DomainException("Only invited participants can accept an invitation.");
        }

        Status = ParticipantStatus.Approved;
        JoinedAt = utcNow;
        CanReview = false;
        Touch(utcNow);
    }

    public void DeclineInvitation(DateTimeOffset utcNow)
    {
        if (Status is not ParticipantStatus.Invited)
        {
            throw new DomainException("Only invited participants can decline an invitation.");
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

        if (IsGuest)
        {
            throw new DomainException("Guest participants cannot be marked as attended.");
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

        if (IsGuest)
        {
            throw new DomainException("Guest participants cannot be marked as no-show.");
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
        return Status is ParticipantStatus.Approved
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

    private static string NormalizeRequiredGuestName(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"Guest {field} name is required.");
        }

        var normalized = value.Trim();

        if (normalized.Length > GuestNameMaxLength)
        {
            throw new DomainException($"Guest name cannot exceed {GuestNameMaxLength} characters.");
        }

        return normalized;
    }
}
