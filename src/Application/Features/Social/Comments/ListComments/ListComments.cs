using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Models;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Social.Comments.ListComments;

public sealed record ListCommentsQuery(Guid PostId, string? Before = null, int Limit = 30)
    : IQuery<CursorPagedResult<CommentResponse>>;

internal sealed class ListCommentsQueryHandler
    : IQueryHandler<ListCommentsQuery, CursorPagedResult<CommentResponse>>
{
    private const int MaxLimit = 100;

    private readonly IApplicationDbContext _dbContext;

    public ListCommentsQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<CursorPagedResult<CommentResponse>>> Handle(
        ListCommentsQuery request,
        CancellationToken cancellationToken)
    {
        var postExists = await _dbContext.Posts.AsNoTracking()
            .AnyAsync(post => post.Id == request.PostId, cancellationToken);

        if (!postExists)
        {
            return Result<CursorPagedResult<CommentResponse>>.Failure(PostErrors.NotFound);
        }

        var limit = request.Limit is < 1 or > MaxLimit ? 30 : request.Limit;

        // Root comments only; clients lazy-load replies separately if needed.
        var query =
            from comment in _dbContext.PostComments.AsNoTracking()
            join profile in _dbContext.UserProfiles.AsNoTracking()
                on comment.UserId equals profile.UserId into profiles
            from profile in profiles.DefaultIfEmpty()
            where comment.PostId == request.PostId
                  && comment.ParentCommentId == null
                  && !comment.IsHidden
            select new { comment, profile };

        if (!string.IsNullOrWhiteSpace(request.Before))
        {
            if (!Guid.TryParse(request.Before, out var beforeId))
            {
                return Result<CursorPagedResult<CommentResponse>>.Failure(PostErrors.InvalidCursor);
            }

            var cursor = await _dbContext.PostComments.AsNoTracking()
                .Where(comment => comment.Id == beforeId)
                .Select(comment => new { comment.CreatedAt, comment.Id })
                .FirstOrDefaultAsync(cancellationToken);

            if (cursor is null)
            {
                return Result<CursorPagedResult<CommentResponse>>.Failure(PostErrors.InvalidCursor);
            }

            var cursorIdKey = cursor.Id.ToString("D");
            query = query.Where(row =>
                row.comment.CreatedAt < cursor.CreatedAt
                || (row.comment.CreatedAt == cursor.CreatedAt
                    && row.comment.Id.ToString().CompareTo(cursorIdKey) < 0));
        }

        var page = await query
            .OrderByDescending(row => row.comment.CreatedAt)
            .ThenByDescending(row => row.comment.Id)
            .Take(limit + 1)
            .Select(row => new CommentResponse(
                row.comment.Id,
                row.comment.PostId,
                row.comment.UserId,
                row.profile != null ? row.profile.Username : null,
                row.profile != null ? row.profile.FirstName : null,
                row.profile != null ? row.profile.ProfileImageUrl : null,
                row.comment.ParentCommentId,
                row.comment.Content,
                row.comment.LikeCount,
                row.comment.ReplyCount,
                row.comment.CreatedAt))
            .ToListAsync(cancellationToken);

        var hasMore = page.Count > limit;
        var items = hasMore ? page.Take(limit).ToList() : page;
        var nextCursor = hasMore && items.Count > 0 ? items[^1].Id.ToString("D") : null;

        return Result<CursorPagedResult<CommentResponse>>.Success(
            CursorPagedResult<CommentResponse>.Create(items, nextCursor));
    }
}
