using System.ComponentModel.DataAnnotations;

namespace SportnerApi.Dtos;

public record RegisterDto(
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password,
    [Required] string FullName
);

public record LoginDto(
    [Required, EmailAddress] string Email,
    [Required] string Password
);

public record AuthResponseDto(
    string Token,
    Guid UserId,
    string Email,
    string? FullName
);
