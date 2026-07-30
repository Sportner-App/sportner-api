namespace Sportner.Application.DTOs.Events;

public record EventDto(
    Guid Id,
    string Title,
    string? Description,
    string SportType,
    DateTime EventDate,
    int MaxPlayers,
    string? AddressText,
    double Latitude,
    double Longitude,
    int ParticipantsCount,
    Guid CreatedBy,
    string? OrganizerName,
    string? OrganizerAvatarUrl,
    DateTime CreatedAt
);

public record EventDetailDto(
    Guid Id,
    string Title,
    string? Description,
    string SportType,
    DateTime EventDate,
    int MaxPlayers,
    string? AddressText,
    double Latitude,
    double Longitude,
    int ParticipantsCount,
    Guid CreatedBy,
    string? OrganizerName,
    string? OrganizerAvatarUrl,
    DateTime CreatedAt,
    int ApprovedParticipantsCount,
    List<ParticipantDto> Participants
);

public record CreateEventDto(
    string Title,
    string? Description,
    string SportType,
    DateTime EventDate,
    int MaxPlayers,
    string? AddressText,
    double Latitude,
    double Longitude
);

public record ParticipantDto(
    Guid UserId,
    string? FullName,
    string? AvatarUrl,
    string? SkillLevel,
    string Status
);

public record UpdateParticipantStatusDto(
    string Status
);

public record EventListQuery(
    string? SportType = null,
    string? Search = null,
    double? Latitude = null,
    double? Longitude = null,
    double? RadiusKm = null,
    string Timeframe = "upcoming"
);
