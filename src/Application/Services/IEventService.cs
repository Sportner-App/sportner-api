using Sportner.Application.DTOs.Auth;
using Sportner.Application.DTOs.Events;

namespace Sportner.Application.Services;

public interface IEventService
{
    Task<IReadOnlyList<EventDto>> GetEventsAsync(EventListQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EventDto>> GetMyPastEventsAsync(CancellationToken cancellationToken = default);
    Task<EventDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EventDto> CreateAsync(CreateEventDto dto, CancellationToken cancellationToken = default);
    Task<MessageResponseDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ParticipantDto> JoinAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ParticipantDto>> GetParticipantsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ParticipantDto> UpdateParticipantStatusAsync(
        Guid id,
        Guid userId,
        UpdateParticipantStatusDto dto,
        CancellationToken cancellationToken = default);
}
