using Sportner.Application.DTOs.Auth;

namespace Sportner.Application.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);
    Task<MessageResponseDto> UpdatePushTokenAsync(UpdatePushTokenDto dto, CancellationToken cancellationToken = default);
}
