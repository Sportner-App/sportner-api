using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Abstractions.Storage;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.UserProfiles.UpdateAvatar;

internal sealed class UpdateAvatarCommandHandler
    : ProfileMediaUploadHandlerBase, ICommandHandler<UpdateAvatarCommand, MyProfileResponse>
{
    private static readonly string[] AllowedContentTypes =
        ["image/jpeg", "image/png", "image/webp"];

    public UpdateAvatarCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        IFileStorage fileStorage)
        : base(dbContext, currentUser, timeProvider, fileStorage)
    {
    }

    public Task<Result<MyProfileResponse>> Handle(
        UpdateAvatarCommand request,
        CancellationToken cancellationToken) =>
        ReplaceMediaAsync(
            StorageBuckets.Avatars,
            AllowedContentTypes,
            request.Content,
            request.ContentType,
            request.FileName,
            profile => profile.ProfileImageUrl,
            (profile, path, utcNow) => profile.UpdateAvatar(path, utcNow),
            cancellationToken);
}
