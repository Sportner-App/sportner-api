using Sportner.Application.DTOs.Messages;

namespace Sportner.Application.Services;

public interface IMessageService
{
    Task<IReadOnlyList<MessageDto>> GetMessagesAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<MessageDto> CreateMessageAsync(Guid eventId, CreateMessageDto dto, CancellationToken cancellationToken = default);
}
