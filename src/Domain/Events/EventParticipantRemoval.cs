using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Events;

public class EventParticipantRemoval : AuditableEntity
{
    public const int NoteMaxLength = 1000;

    private EventParticipantRemoval()
    {
    }

    public Guid EventId { get; private set; }

    public Guid ParticipantId { get; private set; }

    public Guid OrganizerUserId { get; private set; }

    public Guid? RemovedUserId { get; private set; }

    public Guid ReportReasonId { get; private set; }

    public string? Note { get; private set; }

    public static EventParticipantRemoval Create(
        Guid eventId,
        Guid participantId,
        Guid organizerUserId,
        Guid? removedUserId,
        Guid reportReasonId,
        string? note,
        DateTimeOffset utcNow)
    {
        if (eventId == Guid.Empty || participantId == Guid.Empty
            || organizerUserId == Guid.Empty || reportReasonId == Guid.Empty)
        {
            throw new DomainException("Event, participant, organizer and removal reason are required.");
        }

        var normalizedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (normalizedNote?.Length > NoteMaxLength)
        {
            throw new DomainException($"Removal note cannot exceed {NoteMaxLength} characters.");
        }

        return new EventParticipantRemoval
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            ParticipantId = participantId,
            OrganizerUserId = organizerUserId,
            RemovedUserId = removedUserId,
            ReportReasonId = reportReasonId,
            Note = normalizedNote,
            CreatedAt = utcNow
        };
    }
}
