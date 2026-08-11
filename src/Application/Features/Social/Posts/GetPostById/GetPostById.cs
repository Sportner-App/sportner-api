using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Social.Posts.GetPostById;

public sealed record GetPostByIdQuery(Guid PostId) : IQuery<PostResponse>;

internal sealed class GetPostByIdQueryHandler : IQueryHandler<GetPostByIdQuery, PostResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetPostByIdQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<PostResponse>> Handle(
        GetPostByIdQuery request,
        CancellationToken cancellationToken)
    {
        var post = await _dbContext.Posts
            .Include(candidate => candidate.Media)
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == request.PostId, cancellationToken);

        if (post is null)
        {
            return Result<PostResponse>.Failure(PostErrors.NotFound);
        }

        if (post.IsHidden && _currentUser.UserId != post.UserId)
        {
            return Result<PostResponse>.Failure(PostErrors.NotFound);
        }

        if (_currentUser.UserId is { } viewerId)
        {
            var blocked = await SocialQueries.BlockedUserIds(_dbContext, viewerId)
                .AnyAsync(userId => userId == post.UserId, cancellationToken);

            if (blocked)
            {
                return Result<PostResponse>.Failure(PostErrors.Forbidden);
            }
        }

        return Result<PostResponse>.Success(
            await SocialQueries.ToPostResponseAsync(
                _dbContext,
                post,
                _currentUser.UserId,
                cancellationToken));
    }
}
