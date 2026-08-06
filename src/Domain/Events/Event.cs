using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Events;

public class Event : AggregateRoot
{
    private readonly List<EventParticipant> _participants = [];
    private readonly List<EventWaitlist> _waitlist = [];

    private Event()
    {
    }

    public Guid OrganizerUserId { get; private set; }

    public Guid SportId { get; private set; }

    public string Title { get; private set; } = null!;

    public string? Description { get; private set; }

    public DateTimeOffset EventDate { get; private set; }

    public int DurationMinutes { get; private set; }

    public decimal Latitude { get; private set; }

    public decimal Longitude { get; private set; }

    public string Address { get; private set; } = null!;

    public int? MaxParticipants { get; private set; }

    public EventStatus Status { get; private set; }

    public IReadOnlyCollection<EventParticipant> Participants => _participants.AsReadOnly();

    public IReadOnlyCollection<EventWaitlist> Waitlist => _waitlist.AsReadOnly();

    public static Event Create(
        Guid organizerUserId,
        Guid sportId,
        string title,
        DateTimeOffset eventDate,
        int durationMinutes,
        decimal latitude,
        decimal longitude,
        string address,
        DateTimeOffset utcNow,
        string? description = null,
        int? maxParticipants = null)
    {
        if (organizerUserId == Guid.Empty)
        {
            throw new DomainException("Organizer user id is required.");
        }

        if (sportId == Guid.Empty)
        {
            throw new DomainException("Sport id is required.");
        }

        var @event = new Event
        {
            Id = Guid.NewGuid(),
            OrganizerUserId = organizerUserId,
            SportId = sportId,
            Title = NormalizeTitle(title),
            Description = NormalizeOptionalDescription(description),
            EventDate = NormalizeEventDate(eventDate, utcNow),
            DurationMinutes = NormalizeDuration(durationMinutes),
            Latitude = NormalizeLatitude(latitude),
            Longitude = NormalizeLongitude(longitude),
            Address = NormalizeAddress(address),
            MaxParticipants = NormalizeMaxParticipants(maxParticipants),
            Status = EventStatus.Draft,
            CreatedAt = utcNow
        };

        @event._participants.Add(
            EventParticipant.CreateOrganizer(@event.Id, organizerUserId, utcNow));

        return @event;
    }

    public void UpdateDetails(string title, string? description, DateTimeOffset utcNow)
    {
        EnsureEditable();

        Title = NormalizeTitle(title);
        Description = NormalizeOptionalDescription(description);
        Touch(utcNow);
    }

    public void UpdateSchedule(DateTimeOffset eventDate, int durationMinutes, DateTimeOffset utcNow)
    {
        EnsureEditable();

        EventDate = NormalizeEventDate(eventDate, utcNow);
        DurationMinutes = NormalizeDuration(durationMinutes);
        Touch(utcNow);
    }

    public void UpdateLocation(
        decimal latitude,
        decimal longitude,
        string address,
        DateTimeOffset utcNow)
    {
        EnsureEditable();

        Latitude = NormalizeLatitude(latitude);
        Longitude = NormalizeLongitude(longitude);
        Address = NormalizeAddress(address);
        Touch(utcNow);
    }

    public void UpdateCapacity(int? maxParticipants, DateTimeOffset utcNow)
    {
        EnsureEditable();

        var normalized = NormalizeMaxParticipants(maxParticipants);

        if (normalized is not null && OccupiedParticipantCount() > normalized.Value)
        {
            throw new DomainException("Capacity cannot be less than current occupied participant count.");
        }

        MaxParticipants = normalized;
        RefreshCapacityStatus(utcNow);
        Touch(utcNow);
    }

    public void Publish(DateTimeOffset utcNow)
    {
        if (Status is EventStatus.Published or EventStatus.Full)
        {
            return;
        }

        if (Status is not EventStatus.Draft)
        {
            throw new DomainException($"Event cannot be published from status '{Status}'.");
        }

        Status = HasAvailableCapacity() ? EventStatus.Published : EventStatus.Full;
        Touch(utcNow);
    }

    public void Cancel(DateTimeOffset utcNow)
    {
        if (Status is EventStatus.Cancelled)
        {
            return;
        }

        if (Status is EventStatus.Completed)
        {
            throw new DomainException("Completed events cannot be cancelled.");
        }

        if (Status is not (EventStatus.Draft or EventStatus.Published or EventStatus.Full))
        {
            throw new DomainException($"Event cannot be cancelled from status '{Status}'.");
        }

        Status = EventStatus.Cancelled;
        Touch(utcNow);
    }

    public void Complete(DateTimeOffset utcNow)
    {
        if (Status is EventStatus.Completed)
        {
            return;
        }

        if (Status is not (EventStatus.Published or EventStatus.Full))
        {
            throw new DomainException($"Event cannot be completed from status '{Status}'.");
        }

        if (utcNow < GetScheduledEnd())
        {
            throw new DomainException("Event cannot be completed before its scheduled end.");
        }

        Status = EventStatus.Completed;
        Touch(utcNow);
    }

    public (EventParticipant? Participant, EventWaitlist? WaitlistEntry) Apply(
        Guid userId,
        DateTimeOffset utcNow)
    {
        EnsureAcceptsApplications();

        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        if (userId == OrganizerUserId)
        {
            throw new DomainException("Organizer cannot apply to their own event.");
        }

        if (_participants.Any(participant => participant.UserId == userId))
        {
            throw new DomainException("User is already associated with this event.");
        }

        if (_waitlist.Any(entry => entry.UserId == userId))
        {
            throw new DomainException("User is already on the waiting list.");
        }

        if (!HasAvailableCapacity())
        {
            var waitlistEntry = EventWaitlist.Create(
                Id,
                userId,
                NextWaitlistPosition(),
                utcNow);

            _waitlist.Add(waitlistEntry);
            RefreshCapacityStatus(utcNow);
            Touch(utcNow);

            return (null, waitlistEntry);
        }

        var participant = EventParticipant.CreatePending(Id, userId, utcNow);
        _participants.Add(participant);
        Touch(utcNow);

        return (participant, null);
    }

    public void ApproveParticipant(Guid userId, DateTimeOffset utcNow)
    {
        EnsureCanManageApplications();

        var participant = FindParticipant(userId);

        if (participant.Status is not ParticipantStatus.Pending)
        {
            throw new DomainException("Only pending participants can be approved.");
        }

        participant.Approve(utcNow);
        RefreshCapacityStatus(utcNow);
        Touch(utcNow);
    }

    public void RejectParticipant(Guid userId, DateTimeOffset utcNow)
    {
        EnsureCanManageApplications();

        var participant = FindParticipant(userId);
        var occupiedCapacity = participant.OccupiesCapacity();

        participant.Reject(utcNow);

        if (occupiedCapacity)
        {
            RefreshCapacityStatus(utcNow);
        }

        Touch(utcNow);
    }

    public void CancelParticipation(Guid userId, DateTimeOffset utcNow)
    {
        EnsureNotTerminalForParticipationChanges();

        if (userId == OrganizerUserId)
        {
            throw new DomainException("Organizer participation cannot be cancelled.");
        }

        var participant = FindParticipant(userId);
        var occupiedCapacity = participant.OccupiesCapacity();

        participant.Cancel(utcNow);

        if (occupiedCapacity)
        {
            RefreshCapacityStatus(utcNow);
        }

        Touch(utcNow);
    }

    public EventParticipant PromoteFromWaitlist(Guid userId, DateTimeOffset utcNow)
    {
        EnsureCanManageApplications();

        if (!HasAvailableCapacity())
        {
            throw new DomainException("Event capacity is full.");
        }

        var waitlistEntry = FindWaitlistEntry(userId);

        if (waitlistEntry.EventId != Id)
        {
            throw new DomainException("Waitlist entry does not belong to this event.");
        }

        _waitlist.Remove(waitlistEntry);
        ResequenceWaitlist(utcNow);

        var participant = EventParticipant.CreateApproved(Id, userId, utcNow);
        _participants.Add(participant);

        RefreshCapacityStatus(utcNow);
        Touch(utcNow);

        return participant;
    }

    public void ConfirmAttendance(Guid userId, DateTimeOffset utcNow)
    {
        if (Status is not EventStatus.Completed)
        {
            throw new DomainException("Attendance can only be confirmed after the event is completed.");
        }

        var participant = FindParticipant(userId);
        participant.ConfirmAttendance(utcNow);
        Touch(utcNow);
    }

    public void MarkNoShow(Guid userId, DateTimeOffset utcNow)
    {
        if (Status is not EventStatus.Completed)
        {
            throw new DomainException("No-show can only be marked after the event is completed.");
        }

        var participant = FindParticipant(userId);
        participant.MarkNoShow(utcNow);
        Touch(utcNow);
    }

    public int OccupiedParticipantCount()
    {
        return _participants.Count(participant => participant.OccupiesCapacity());
    }

    public bool HasAvailableCapacity()
    {
        return MaxParticipants is null
            || OccupiedParticipantCount() < MaxParticipants.Value;
    }

    private DateTimeOffset GetScheduledEnd()
    {
        return EventDate.AddMinutes(DurationMinutes);
    }

    private void RefreshCapacityStatus(DateTimeOffset utcNow)
    {
        if (Status is EventStatus.Completed or EventStatus.Cancelled or EventStatus.Draft)
        {
            return;
        }

        if (MaxParticipants is null)
        {
            if (Status is EventStatus.Full)
            {
                Status = EventStatus.Published;
            }

            return;
        }

        if (!HasAvailableCapacity())
        {
            Status = EventStatus.Full;
            return;
        }

        if (Status is EventStatus.Full)
        {
            Status = EventStatus.Published;
        }
    }

    private int NextWaitlistPosition()
    {
        return _waitlist.Count == 0
            ? 1
            : _waitlist.Max(entry => entry.Position) + 1;
    }

    private void ResequenceWaitlist(DateTimeOffset utcNow)
    {
        var ordered = _waitlist
            .OrderBy(entry => entry.Position)
            .ThenBy(entry => entry.CreatedAt)
            .ToList();

        for (var index = 0; index < ordered.Count; index++)
        {
            ordered[index].ChangePosition(index + 1, utcNow);
        }
    }

    private EventParticipant FindParticipant(Guid userId)
    {
        var participant = _participants.FirstOrDefault(item => item.UserId == userId);

        if (participant is null)
        {
            throw new DomainException("Participant was not found.");
        }

        if (participant.EventId != Id)
        {
            throw new DomainException("Participant does not belong to this event.");
        }

        return participant;
    }

    private EventWaitlist FindWaitlistEntry(Guid userId)
    {
        var entry = _waitlist.FirstOrDefault(item => item.UserId == userId);

        if (entry is null)
        {
            throw new DomainException("Waitlist entry was not found.");
        }

        return entry;
    }

    private void EnsureEditable()
    {
        if (Status is not (EventStatus.Draft or EventStatus.Published or EventStatus.Full))
        {
            throw new DomainException($"Event cannot be updated while status is '{Status}'.");
        }
    }

    private void EnsureAcceptsApplications()
    {
        if (Status is not (EventStatus.Published or EventStatus.Full))
        {
            throw new DomainException("Event does not accept applications in the current status.");
        }
    }

    private void EnsureCanManageApplications()
    {
        if (Status is not (EventStatus.Published or EventStatus.Full))
        {
            throw new DomainException("Applications cannot be managed in the current event status.");
        }
    }

    private void EnsureNotTerminalForParticipationChanges()
    {
        if (Status is EventStatus.Cancelled or EventStatus.Completed)
        {
            throw new DomainException("Participation cannot be changed for a completed or cancelled event.");
        }

        if (Status is EventStatus.Draft)
        {
            throw new DomainException("Participation cannot be cancelled while the event is a draft.");
        }
    }

    private void Touch(DateTimeOffset utcNow)
    {
        UpdatedAt = utcNow;
    }

    private static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Event title is required.");
        }

        var normalized = title.Trim();

        if (normalized.Length > 150)
        {
            throw new DomainException("Event title cannot exceed 150 characters.");
        }

        return normalized;
    }

    private static string? NormalizeOptionalDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        return description.Trim();
    }

    private static DateTimeOffset NormalizeEventDate(DateTimeOffset eventDate, DateTimeOffset utcNow)
    {
        if (eventDate <= utcNow)
        {
            throw new DomainException("Event date must be later than the current time.");
        }

        return eventDate;
    }

    private static int NormalizeDuration(int durationMinutes)
    {
        if (durationMinutes <= 0)
        {
            throw new DomainException("Duration must be greater than zero.");
        }

        return durationMinutes;
    }

    private static decimal NormalizeLatitude(decimal latitude)
    {
        if (latitude is < -90m or > 90m)
        {
            throw new DomainException("Latitude must be between -90 and 90.");
        }

        return decimal.Round(latitude, 6, MidpointRounding.AwayFromZero);
    }

    private static decimal NormalizeLongitude(decimal longitude)
    {
        if (longitude is < -180m or > 180m)
        {
            throw new DomainException("Longitude must be between -180 and 180.");
        }

        return decimal.Round(longitude, 6, MidpointRounding.AwayFromZero);
    }

    private static string NormalizeAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            throw new DomainException("Address is required.");
        }

        return address.Trim();
    }

    private static int? NormalizeMaxParticipants(int? maxParticipants)
    {
        if (maxParticipants is null)
        {
            return null;
        }

        if (maxParticipants <= 0)
        {
            throw new DomainException("Max participants must be greater than zero when provided.");
        }

        return maxParticipants;
    }

}
