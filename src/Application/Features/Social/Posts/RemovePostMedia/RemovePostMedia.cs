using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Abstractions.Storage;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Social.Posts.RemovePostMedia;

public sealed record RemovePostMediaCommand(Guid PostId, Guid MediaId) : ICommand<PostResponse>;

internal sealed class RemovePostMediaCommandHandler
    : ICommandHandler<RemovePostMediaCommand, PostResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly IFileStorage _fileStorage;

    public RemovePostMediaCommandHandler(
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
        RemovePostMediaCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<PostResponse>.Failure(PostErrors.NotAuthenticated);
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

        if (post.Media.All(item => item.Id != request.MediaId))
        {
            return Result<PostResponse>.Failure(PostErrors.MediaNotFound);
        }

        var storagePath = post.RemoveMedia(request.MediaId, _timeProvider.GetUtcNow());
        await _dbContext.SaveChangesAsync(cancellationToken);

        await StorageCleanup.TryDeleteAsync(
            _fileStorage,
            StorageBuckets.PostMedia,
            storagePath,
            cancellationToken);

        return Result<PostResponse>.Success(
            await SocialQueries.ToPostResponseAsync(_dbContext, post, userId, cancellationToken));
    }
}
