using Sportner.Application.DTOs.Messages;
using Sportner.Domain.Entities;

namespace Sportner.Application.Mappers;

public static class MessageMapper
{
    public static MessageDto ToDto(this Message message) => new(
        message.Id,
        message.EventId,
        message.UserId,
        message.User?.FullName,
        message.User?.AvatarUrl,
        message.Content,
        message.CreatedAt
    );
}
