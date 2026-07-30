using System.ComponentModel.DataAnnotations;

namespace SportnerApi.Dtos;

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
    [Required] string Title,
    string? Description,
    [Required] string SportType,
    [Required] DateTime EventDate,
    [Range(2, 100)] int MaxPlayers,
    string? AddressText,
    [Required] double Latitude,
    [Required] double Longitude
);

public record ParticipantDto(
    Guid UserId,
    string? FullName,
    string? AvatarUrl,
    string? SkillLevel,
    string Status
);

public record UpdateParticipantStatusDto(
    [Required] string Status
);
