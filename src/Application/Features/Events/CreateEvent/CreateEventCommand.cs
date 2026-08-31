using Sportner.Application.Abstractions.Messaging;

namespace Sportner.Application.Features.Events.CreateEvent;

public sealed record CreateEventCommand(
    Guid SportId,
    string Title,
    string? Description,
    DateTimeOffset EventDate,
    int DurationMinutes,
    decimal Latitude,
    decimal Longitude,
    string Address,
    int? MaxParticipants,
    int MinParticipantAge,
    int MaxParticipantAge,
    short? SkillLevel = null) : ICommand<EventResponse>;
