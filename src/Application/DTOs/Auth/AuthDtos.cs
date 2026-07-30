namespace Sportner.Application.DTOs.Auth;

public record RegisterDto(
    string Email,
    string Password,
    string FullName
);

public record LoginDto(
    string Email,
    string Password
);

public record AuthResponseDto(
    string Token,
    Guid UserId,
    string Email,
    string? FullName
);

public record UpdatePushTokenDto(
    string PushToken
);

public record MessageResponseDto(
    string Message
);
