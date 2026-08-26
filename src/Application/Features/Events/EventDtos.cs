namespace Sportner.Application.Features.Events;

public sealed record OrganizerSnippetResponse(
    Guid UserId,
    string? Username,
    string? FirstName,
    string? LastName,
    string? ProfileImageUrl);

public sealed record EventResponse(
    Guid Id,
    Guid SportId,
    string SportName,
    string SportSlug,
    string? SportCoverImageUrl,
    OrganizerSnippetResponse Organizer,
    string Title,
    string? Description,
    DateTimeOffset EventDate,
    int DurationMinutes,
    decimal Latitude,
    decimal Longitude,
    string Address,
    int? MaxParticipants,
    short Status,
    int OccupiedParticipantCount,
    int WaitlistCount,
    short? MyParticipationStatus,
    bool IsOnWaitlist,
    Guid? ConversationId);

public sealed record EventListItemResponse(
    Guid Id,
    Guid SportId,
    string SportName,
    string SportSlug,
    string? SportCoverImageUrl,
    Guid OrganizerUserId,
    string? OrganizerUsername,
    string Title,
    DateTimeOffset EventDate,
    int DurationMinutes,
    string Address,
    int? MaxParticipants,
    short Status,
    int OccupiedParticipantCount);

public sealed record ParticipantResponse(
    Guid Id,
    Guid? UserId,
    short Kind,
    bool IsGuest,
    string? Username,
    string? FirstName,
    string? LastName,
    string? ProfileImageUrl,
    short Status,
    DateTimeOffset? JoinedAt,
    DateTimeOffset? AttendedAt,
    bool CanReview);

public sealed record WaitlistEntryResponse(
    Guid UserId,
    string? Username,
    string? FirstName,
    string? LastName,
    int Position,
    DateTimeOffset CreatedAt);

public sealed record ApplyToEventResponse(
    bool JoinedWaitlist,
    short? ParticipantStatus,
    int? WaitlistPosition);
