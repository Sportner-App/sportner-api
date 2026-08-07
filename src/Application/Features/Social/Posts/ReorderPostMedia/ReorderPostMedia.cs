using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Social.Posts.ReorderPostMedia;

public sealed record ReorderPostMediaCommand(Guid PostId, IReadOnlyList<Guid> OrderedMediaIds)
    : ICommand<PostResponse>;

internal sealed class ReorderPostMediaCommandHandler
    : ICommandHandler<ReorderPostMediaCommand, PostResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public ReorderPostMediaCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<PostResponse>> Handle(
        ReorderPostMediaCommand request,
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

        post.ReorderMedia(request.OrderedMediaIds, _timeProvider.GetUtcNow());
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<PostResponse>.Success(
            await SocialQueries.ToPostResponseAsync(_dbContext, post, userId, cancellationToken));
    }
}
