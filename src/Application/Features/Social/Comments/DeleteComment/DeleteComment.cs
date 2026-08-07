using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Social.Comments.DeleteComment;

public sealed record DeleteCommentCommand(Guid CommentId) : ICommand;

internal sealed class DeleteCommentCommandHandler : ICommandHandler<DeleteCommentCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public DeleteCommentCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result.Failure(PostErrors.NotAuthenticated);
        }

        var comment = await _dbContext.PostComments
            .FirstOrDefaultAsync(candidate => candidate.Id == request.CommentId, cancellationToken);

        if (comment is null)
        {
            return Result.Failure(PostErrors.CommentNotFound);
        }

        if (comment.UserId != userId)
        {
            return Result.Failure(PostErrors.NotOwner);
        }

        var post = await _dbContext.Posts
            .FirstOrDefaultAsync(candidate => candidate.Id == comment.PostId, cancellationToken);

        if (post is null)
        {
            return Result.Failure(PostErrors.NotFound);
        }

        var utcNow = _timeProvider.GetUtcNow();
        var deleteCount = 1;

        if (!comment.IsReply())
        {
            var replies = await _dbContext.PostComments
                .Where(candidate => candidate.ParentCommentId == comment.Id)
                .ToListAsync(cancellationToken);

            deleteCount += replies.Count;
            _dbContext.PostComments.RemoveRange(replies);
        }
        else
        {
            var parent = await _dbContext.PostComments
                .FirstOrDefaultAsync(
                    candidate => candidate.Id == comment.ParentCommentId,
                    cancellationToken);

            if (parent is not null && parent.ReplyCount > 0)
            {
                parent.DecrementReplyCount(utcNow);
            }
        }

        _dbContext.PostComments.Remove(comment);

        if (post.CommentCount >= deleteCount)
        {
            post.DecrementCommentCount(utcNow, deleteCount);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
