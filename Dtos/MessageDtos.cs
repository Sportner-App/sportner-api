using System.ComponentModel.DataAnnotations;

namespace SportnerApi.Dtos;

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
    [Required, MinLength(1), MaxLength(2000)] string Content
);
