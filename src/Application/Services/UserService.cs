using System.Net;
using Sportner.Application.Abstractions;
using Sportner.Application.DTOs.Users;
using Sportner.Application.Helpers;
using Sportner.Application.Mappers;
using Sportner.Domain.Abstractions;
using Sportner.Domain.Data.Interfaces;
using Sportner.Domain.Exceptions;
using Sportner.Localization.Resources;

namespace Sportner.Application.Services;

public class UserService(
    IUnitOfWork unitOfWork,
    IStorageService storageService,
    ICurrentUser currentUser) : IUserService
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp"
    };

    private const long MaxAvatarBytes = 5 * 1024 * 1024; // 5 MB

    public async Task<UserDto> GetMeAsync(CancellationToken cancellationToken = default)
    {
        var userId = RequireUserId();

        var user = await unitOfWork.Users.FindOneAsync(p => p.Id == userId, cancellationToken)
            ?? throw new ApiException(HttpStatusCode.NotFound, ValidationResource.Exception_Profile_NotFound);

        return user.ToDto(includePushToken: true);
    }

    public async Task<UserDto> UpdateMeAsync(
        UpdateUserDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = RequireUserId();

        var user = await unitOfWork.Users.FindOneAsync(p => p.Id == userId, cancellationToken)
            ?? throw new ApiException(HttpStatusCode.NotFound, ValidationResource.Exception_Profile_NotFound);

        if (dto.FullName is not null)
        {
            user.FullName = dto.FullName.Trim();
        }

        if (dto.AvatarUrl is not null)
        {
            user.AvatarUrl = string.IsNullOrWhiteSpace(dto.AvatarUrl)
                ? null
                : dto.AvatarUrl.Trim();
        }

        if (dto.Bio is not null)
        {
            user.Bio = string.IsNullOrWhiteSpace(dto.Bio)
                ? null
                : dto.Bio.Trim();
        }

        if (dto.Sports is not null)
        {
            user.Sports = dto.Sports;
        }

        if (dto.IntroVideoUrl is not null)
        {
            user.IntroVideoUrl = string.IsNullOrWhiteSpace(dto.IntroVideoUrl)
                ? null
                : dto.IntroVideoUrl.Trim();
        }

        if (dto.BirthDate is not null)
        {
            user.BirthDate = SkillLevelHelper.ToUtc(dto.BirthDate.Value);
        }

        if (dto.IsOnboarded is not null)
        {
            user.IsOnboarded = dto.IsOnboarded.Value;
        }

        if (dto.SkillLevels is not null)
        {
            user.SkillLevels = SkillLevelHelper.ToJsonbString(dto.SkillLevels.Value);
        }

        user.UpdatedAt = DateTime.UtcNow;
        unitOfWork.Users.UpdateOne(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return user.ToDto(includePushToken: true);
    }

    public async Task<AvatarUploadResponseDto> UploadAvatarAsync(
        Guid userId,
        Stream stream,
        string contentType,
        string extension,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(contentType) || !AllowedContentTypes.Contains(contentType))
        {
            throw new ApiException(HttpStatusCode.BadRequest, ValidationResource.Exception_Profile_AvatarInvalidType);
        }

        if (stream.CanSeek && stream.Length > MaxAvatarBytes)
        {
            throw new ApiException(HttpStatusCode.BadRequest, ValidationResource.Exception_Profile_AvatarTooLarge);
        }

        if (stream.CanSeek && stream.Length == 0)
        {
            throw new ApiException(HttpStatusCode.BadRequest, ValidationResource.Exception_Profile_AvatarRequired);
        }

        var user = await unitOfWork.Users.FindOneAsync(p => p.Id == userId, cancellationToken)
            ?? throw new ApiException(HttpStatusCode.NotFound, ValidationResource.Exception_Profile_NotFound);

        string avatarUrl;
        try
        {
            avatarUrl = await storageService.UploadAvatarAsync(
                userId,
                stream,
                contentType,
                extension,
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new ApiException(HttpStatusCode.BadRequest, ex.Message);
        }

        user.AvatarUrl = avatarUrl;
        user.UpdatedAt = DateTime.UtcNow;
        unitOfWork.Users.UpdateOne(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AvatarUploadResponseDto(avatarUrl);
    }

    public async Task<UserDto> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await unitOfWork.Users.FindOneAsync(p => p.Id == userId, cancellationToken)
            ?? throw new ApiException(HttpStatusCode.NotFound, ValidationResource.Exception_Profile_NotFound);

        return user.ToDto(includePushToken: false);
    }

    private Guid RequireUserId() =>
        currentUser.UserId
        ?? throw new ApiException(HttpStatusCode.Unauthorized, ValidationResource.Exception_Unauthorized);
}
