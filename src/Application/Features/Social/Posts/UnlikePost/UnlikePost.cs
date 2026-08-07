using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Social.Posts.UnlikePost;

public sealed record UnlikePostCommand(Guid PostId) : ICommand;

internal sealed class UnlikePostCommandHandler : ICommandHandler<UnlikePostCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public UnlikePostCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(UnlikePostCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result.Failure(PostErrors.NotAuthenticated);
        }

        var post = await _dbContext.Posts
            .FirstOrDefaultAsync(candidate => candidate.Id == request.PostId, cancellationToken);

        if (post is null)
        {
            return Result.Failure(PostErrors.NotFound);
        }

        var like = await _dbContext.PostLikes
            .FirstOrDefaultAsync(
                candidate => candidate.PostId == post.Id && candidate.UserId == userId,
                cancellationToken);

        if (like is null)
        {
            return Result.Failure(PostErrors.NotLiked);
        }

        _dbContext.PostLikes.Remove(like);

        if (post.LikeCount > 0)
        {
            post.DecrementLikeCount(_timeProvider.GetUtcNow());
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
