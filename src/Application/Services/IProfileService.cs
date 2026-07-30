using Sportner.Application.DTOs.Profiles;

namespace Sportner.Application.Services;

public interface IProfileService
{
    Task<UserProfileDto> GetMeAsync(CancellationToken cancellationToken = default);
    Task<UserProfileDto> UpdateMeAsync(UpdateProfileDto dto, CancellationToken cancellationToken = default);
    Task<AvatarUploadResponseDto> UploadAvatarAsync(
        Guid userId,
        Stream stream,
        string contentType,
        string extension,
        CancellationToken cancellationToken = default);
    Task<UserProfileDto> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
