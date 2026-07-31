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
    Task<UserEventDto> JoinAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserEventDto>> GetParticipantsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserEventDto> UpdateParticipantStatusAsync(
        Guid id,
        Guid userId,
        UpdateUserEventStatusDto dto,
        CancellationToken cancellationToken = default);
}
