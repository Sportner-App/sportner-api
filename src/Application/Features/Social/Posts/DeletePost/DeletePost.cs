using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Abstractions.Storage;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Social.Posts.DeletePost;

public sealed record DeletePostCommand(Guid PostId) : ICommand;

internal sealed class DeletePostCommandHandler : ICommandHandler<DeletePostCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly IFileStorage _fileStorage;

    public DeletePostCommandHandler(
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

    public async Task<Result> Handle(DeletePostCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result.Failure(PostErrors.NotAuthenticated);
        }

        var post = await _dbContext.Posts
            .Include(candidate => candidate.Media)
            .FirstOrDefaultAsync(candidate => candidate.Id == request.PostId, cancellationToken);

        if (post is null)
        {
            return Result.Failure(PostErrors.NotFound);
        }

        if (post.UserId != userId)
        {
            return Result.Failure(PostErrors.NotOwner);
        }

        var storagePaths = post.Media.Select(item => item.StoragePath).ToList();

        var likes = await _dbContext.PostLikes
            .Where(like => like.PostId == post.Id)
            .ToListAsync(cancellationToken);

        var comments = await _dbContext.PostComments
            .Where(comment => comment.PostId == post.Id)
            .ToListAsync(cancellationToken);

        _dbContext.PostLikes.RemoveRange(likes);
        // Delete replies before roots to satisfy parent Restrict FKs if present.
        _dbContext.PostComments.RemoveRange(comments.Where(comment => comment.ParentCommentId is not null));
        _dbContext.PostComments.RemoveRange(comments.Where(comment => comment.ParentCommentId is null));
        _dbContext.Posts.Remove(post);

        var statistics = await _dbContext.UserStatistics
            .FirstOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken);

        if (statistics is not null && statistics.PostsCount > 0)
        {
            statistics.DecreasePostsCount(_timeProvider.GetUtcNow());
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await StorageCleanup.TryDeleteManyAsync(
            _fileStorage,
            StorageBuckets.PostMedia,
            storagePaths,
            cancellationToken);

        return Result.Success();
    }
}
