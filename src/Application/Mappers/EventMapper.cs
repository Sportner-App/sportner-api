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
        var approvedStatus = UserEventStatus.Approved.ToDbValue();
        var participants = eventEntity.UserEvents
            .Select(p => p.ToUserEventDto(eventEntity.SportType))
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

    public static UserEventDto ToUserEventDto(this UserEvent userEvent, string sportType) => new(
        userEvent.UserId,
        userEvent.User?.FullName,
        userEvent.User?.AvatarUrl,
        SkillLevelHelper.ResolveSkillLevel(userEvent.User?.SkillLevels, sportType),
        userEvent.Status
    );
}
