namespace Sportner.Application.DTOs.Messages;

public record MessageDto(
    Guid Id,
    Guid EventId,
    Guid UserId,
    string? UserFullName,
    string? UserAvatarUrl,
    string Content,
    DateTime CreatedAt
);

public record CreateMessageDto(
    string Content
);
