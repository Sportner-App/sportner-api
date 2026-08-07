using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Abstractions.Storage;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.UserProfiles.UpdateIntroVideo;

internal sealed class UpdateIntroVideoCommandHandler
    : ProfileMediaUploadHandlerBase, ICommandHandler<UpdateIntroVideoCommand, MyProfileResponse>
{
    private static readonly string[] AllowedContentTypes =
        ["video/mp4", "video/quicktime", "video/webm"];

    public UpdateIntroVideoCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        IFileStorage fileStorage)
        : base(dbContext, currentUser, timeProvider, fileStorage)
    {
    }

    public Task<Result<MyProfileResponse>> Handle(
        UpdateIntroVideoCommand request,
        CancellationToken cancellationToken) =>
        ReplaceMediaAsync(
            StorageBuckets.IntroVideos,
            AllowedContentTypes,
            request.Content,
            request.ContentType,
            request.FileName,
            profile => profile.IntroVideoUrl,
            (profile, path, utcNow) => profile.UpdateIntroVideo(path, utcNow),
            cancellationToken);
}
