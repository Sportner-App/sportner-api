using System.Net;
using Sportner.Application.Abstractions;
using Sportner.Application.DTOs.Profiles;
using Sportner.Application.Helpers;
using Sportner.Application.Mappers;
using Sportner.Domain.Abstractions;
using Sportner.Domain.Data.Interfaces;
using Sportner.Domain.Exceptions;
using Sportner.Localization.Resources;

namespace Sportner.Application.Services;

public class ProfileService(
    IUnitOfWork unitOfWork,
    IStorageService storageService,
    ICurrentUser currentUser) : IProfileService
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp"
    };

    private const long MaxAvatarBytes = 5 * 1024 * 1024; // 5 MB

    public async Task<UserProfileDto> GetMeAsync(CancellationToken cancellationToken = default)
    {
        var userId = RequireUserId();

        var profile = await unitOfWork.Profiles.FindOneAsync(p => p.Id == userId, cancellationToken)
            ?? throw new ApiException(HttpStatusCode.NotFound, ValidationResource.Exception_Profile_NotFound);

        return profile.ToDto(includePushToken: true);
    }

    public async Task<UserProfileDto> UpdateMeAsync(
        UpdateProfileDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = RequireUserId();

        var profile = await unitOfWork.Profiles.FindOneAsync(p => p.Id == userId, cancellationToken)
            ?? throw new ApiException(HttpStatusCode.NotFound, ValidationResource.Exception_Profile_NotFound);

        if (dto.FullName is not null)
        {
            profile.FullName = dto.FullName.Trim();
        }

        if (dto.AvatarUrl is not null)
        {
            profile.AvatarUrl = string.IsNullOrWhiteSpace(dto.AvatarUrl)
                ? null
                : dto.AvatarUrl.Trim();
        }

        if (dto.Bio is not null)
        {
            profile.Bio = string.IsNullOrWhiteSpace(dto.Bio)
                ? null
                : dto.Bio.Trim();
        }

        if (dto.Sports is not null)
        {
            profile.Sports = dto.Sports;
        }

        if (dto.IntroVideoUrl is not null)
        {
            profile.IntroVideoUrl = string.IsNullOrWhiteSpace(dto.IntroVideoUrl)
                ? null
                : dto.IntroVideoUrl.Trim();
        }

        if (dto.BirthDate is not null)
        {
            profile.BirthDate = SkillLevelHelper.ToUtc(dto.BirthDate.Value);
        }

        if (dto.IsOnboarded is not null)
        {
            profile.IsOnboarded = dto.IsOnboarded.Value;
        }

        if (dto.SkillLevels is not null)
        {
            profile.SkillLevels = SkillLevelHelper.ToJsonbString(dto.SkillLevels.Value);
        }

        profile.UpdatedAt = DateTime.UtcNow;
        unitOfWork.Profiles.UpdateOne(profile);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return profile.ToDto(includePushToken: true);
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

        var profile = await unitOfWork.Profiles.FindOneAsync(p => p.Id == userId, cancellationToken)
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

        profile.AvatarUrl = avatarUrl;
        profile.UpdatedAt = DateTime.UtcNow;
        unitOfWork.Profiles.UpdateOne(profile);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AvatarUploadResponseDto(avatarUrl);
    }

    public async Task<UserProfileDto> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var profile = await unitOfWork.Profiles.FindOneAsync(p => p.Id == userId, cancellationToken)
            ?? throw new ApiException(HttpStatusCode.NotFound, ValidationResource.Exception_Profile_NotFound);

        return profile.ToDto(includePushToken: false);
    }

    private Guid RequireUserId() =>
        currentUser.UserId
        ?? throw new ApiException(HttpStatusCode.Unauthorized, ValidationResource.Exception_Unauthorized);
}
