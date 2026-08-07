using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Abstractions.Storage;
using Sportner.Application.Common.Results;
using Sportner.Domain.Users;

namespace Sportner.Application.Features.Identity.Profiles;

/// <summary>
/// Uploads a profile media file to object storage and stores only the resulting path.
/// Passing a null stream clears the current media.
/// </summary>
internal abstract class ProfileMediaUploadHandlerBase : ProfileUpdateHandlerBase
{
    private readonly IFileStorage _fileStorage;

    protected ProfileMediaUploadHandlerBase(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        IFileStorage fileStorage)
        : base(dbContext, currentUser, timeProvider)
    {
        _fileStorage = fileStorage;
    }

    protected async Task<Result<MyProfileResponse>> ReplaceMediaAsync(
        string bucket,
        IReadOnlyCollection<string> allowedContentTypes,
        Stream? content,
        string? contentType,
        string? fileName,
        Func<Profile, string?> readCurrentPath,
        Action<Profile, string?, DateTimeOffset> apply,
        CancellationToken cancellationToken)
    {
        if (CurrentUser.UserId is not { } userId)
        {
            return Result<MyProfileResponse>.Failure(ProfileErrors.NotAuthenticated);
        }

        string? storedPath = null;

        if (content is not null)
        {
            if (contentType is null || !allowedContentTypes.Contains(contentType))
            {
                return Result<MyProfileResponse>.Failure(ProfileErrors.InvalidMedia);
            }

            var extension = Path.GetExtension(fileName);
            var objectPath = $"{userId}/{Guid.NewGuid():N}{extension}";

            storedPath = await _fileStorage.UploadAsync(
                bucket,
                objectPath,
                content,
                contentType,
                cancellationToken);
        }

        string? previousPath = null;

        var result = await UpdateAsync(
            (profile, utcNow) =>
            {
                previousPath = readCurrentPath(profile);
                apply(profile, storedPath, utcNow);
                return Result.Success();
            },
            cancellationToken);

        if (result.IsSuccess
            && !string.Equals(previousPath, storedPath, StringComparison.Ordinal))
        {
            await StorageCleanup.TryDeleteAsync(
                _fileStorage,
                bucket,
                previousPath,
                cancellationToken);
        }

        return result;
    }
}
