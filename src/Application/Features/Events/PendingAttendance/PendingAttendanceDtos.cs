namespace Sportner.Application.Features.Events.PendingAttendance;

public sealed record PendingAttendanceParticipantResponse(
    Guid UserId,
    string? Username,
    string? FirstName,
    string? ProfileImageUrl);

public sealed record PendingAttendanceEventResponse(
    Guid EventId,
    string Title,
    DateTimeOffset EventDate,
    IReadOnlyList<PendingAttendanceParticipantResponse> Participants);
