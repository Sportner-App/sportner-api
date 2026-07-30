using Sportner.Application.DTOs.Events;
using Sportner.Application.Helpers;
using Sportner.Domain.Entities;
using Sportner.Domain.Enums;

namespace Sportner.Application.Mappers;

public static class EventMapper
{
    public static EventDto ToDto(this Event e) => new(
        e.Id,
        e.Title,
        e.Description,
        e.SportType,
        e.EventDate,
        e.MaxPlayers,
        e.AddressText,
        e.Latitude,
        e.Longitude,
        e.ParticipantsCount,
        e.CreatedBy,
        e.Organizer?.FullName,
        e.Organizer?.AvatarUrl,
        e.CreatedAt
    );

    public static EventDetailDto ToDetailDto(this Event eventEntity)
    {
        var approvedStatus = ParticipantStatus.Approved.ToDbValue();
        var participants = eventEntity.Participants
            .Select(p => p.ToParticipantDto(eventEntity.SportType))
            .ToList();

        return new EventDetailDto(
            eventEntity.Id,
            eventEntity.Title,
            eventEntity.Description,
            eventEntity.SportType,
            eventEntity.EventDate,
            eventEntity.MaxPlayers,
            eventEntity.AddressText,
            eventEntity.Latitude,
            eventEntity.Longitude,
            eventEntity.ParticipantsCount,
            eventEntity.CreatedBy,
            eventEntity.Organizer?.FullName,
            eventEntity.Organizer?.AvatarUrl,
            eventEntity.CreatedAt,
            participants.Count(p => p.Status == approvedStatus),
            participants
        );
    }

    public static ParticipantDto ToParticipantDto(this EventParticipant participant, string sportType) => new(
        participant.UserId,
        participant.User?.FullName,
        participant.User?.AvatarUrl,
        SkillLevelHelper.ResolveSkillLevel(participant.User?.SkillLevels, sportType),
        participant.Status
    );
}
