using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Abstractions.Storage;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Social;

namespace Sportner.Application.Features.Social.Posts.AddPostMedia;

public sealed record AddPostMediaCommand(
    Guid PostId,
    Stream Content,
    string ContentType,
    string FileName,
    long FileSize) : ICommand<PostResponse>;

internal sealed class AddPostMediaCommandHandler : ICommandHandler<AddPostMediaCommand, PostResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly IFileStorage _fileStorage;

    public AddPostMediaCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        IFileStorage fileStorage)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _fileStorage = fileStorage;
    }

    public async Task<Result<PostResponse>> Handle(
        AddPostMediaCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<PostResponse>.Failure(PostErrors.NotAuthenticated);
        }

        if (!PostMediaContentTypes.TryResolve(
                request.ContentType,
                request.FileName,
                out var contentType,
                out var mediaType)
            || request.FileSize <= 0)
        {
            return Result<PostResponse>.Failure(PostErrors.InvalidMedia);
        }

        var post = await _dbContext.Posts
            .Include(candidate => candidate.Media)
            .FirstOrDefaultAsync(candidate => candidate.Id == request.PostId, cancellationToken);

        if (post is null)
        {
            return Result<PostResponse>.Failure(PostErrors.NotFound);
        }

        if (post.UserId != userId)
        {
            return Result<PostResponse>.Failure(PostErrors.NotOwner);
        }

        var utcNow = _timeProvider.GetUtcNow();
        var extension = PostMediaContentTypes.ResolveExtension(request.FileName, contentType);
        var objectPath = $"{userId}/{post.Id}/{Guid.NewGuid():N}{extension}";

        var storedPath = await _fileStorage.UploadAsync(
            StorageBuckets.PostMedia,
            objectPath,
            request.Content,
            contentType,
            cancellationToken);

        post.AddMedia(
            mediaType,
            storedPath,
            request.FileName,
            contentType,
            request.FileSize,
            utcNow);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<PostResponse>.Success(
            await SocialQueries.ToPostResponseAsync(
                _dbContext,
                _fileStorage,
                post,
                userId,
                cancellationToken));
    }
}
