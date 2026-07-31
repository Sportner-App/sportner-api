using Sportner.Application.DTOs.Users;

namespace Sportner.Application.Services;

public interface IUserService
{
    Task<UserDto> GetMeAsync(CancellationToken cancellationToken = default);
    Task<UserDto> UpdateMeAsync(UpdateUserDto dto, CancellationToken cancellationToken = default);
    Task<AvatarUploadResponseDto> UploadAvatarAsync(
        Guid userId,
        Stream stream,
        string contentType,
        string extension,
        CancellationToken cancellationToken = default);
    Task<UserDto> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
