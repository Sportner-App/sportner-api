using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Abstractions.Storage;
using Sportner.Application.Common.Models;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Social.Feed.GetHomeFeed;

public sealed record GetHomeFeedQuery(string? Before = null, int Limit = 20)
    : IQuery<CursorPagedResult<PostResponse>>;

internal sealed class GetHomeFeedQueryHandler
    : IQueryHandler<GetHomeFeedQuery, CursorPagedResult<PostResponse>>
{
    private const int MaxLimit = 50;

    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IFileStorage _fileStorage;

    public GetHomeFeedQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IFileStorage fileStorage)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
    }

    public async Task<Result<CursorPagedResult<PostResponse>>> Handle(
        GetHomeFeedQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<CursorPagedResult<PostResponse>>.Failure(PostErrors.NotAuthenticated);
        }

        var limit = request.Limit is < 1 or > MaxLimit ? 20 : request.Limit;
        var friendIds = SocialQueries.AcceptedFriendIds(_dbContext, userId);
        var blockedIds = SocialQueries.BlockedUserIds(_dbContext, userId);

        var query = _dbContext.Posts.AsNoTracking()
            .Include(post => post.Media)
            .Where(post =>
                (post.UserId == userId || friendIds.Contains(post.UserId))
                && !blockedIds.Contains(post.UserId)
                && (!post.IsHidden || post.UserId == userId));

        if (!string.IsNullOrWhiteSpace(request.Before))
        {
            if (!Guid.TryParse(request.Before, out var beforeId))
            {
                return Result<CursorPagedResult<PostResponse>>.Failure(PostErrors.InvalidCursor);
            }

            var cursor = await _dbContext.Posts.AsNoTracking()
                .Where(post => post.Id == beforeId)
                .Select(post => new { post.CreatedAt, post.Id })
                .FirstOrDefaultAsync(cancellationToken);

            if (cursor is null)
            {
                return Result<CursorPagedResult<PostResponse>>.Failure(PostErrors.InvalidCursor);
            }

            var cursorIdKey = cursor.Id.ToString("D");
            query = query.Where(post =>
                post.CreatedAt < cursor.CreatedAt
                || (post.CreatedAt == cursor.CreatedAt
                    && post.Id.ToString().CompareTo(cursorIdKey) < 0));
        }

        var page = await query
            .OrderByDescending(post => post.CreatedAt)
            .ThenByDescending(post => post.Id)
            .Take(limit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = page.Count > limit;
        var window = hasMore ? page.Take(limit).ToList() : page;

        var items = new List<PostResponse>(window.Count);
        foreach (var post in window)
        {
            items.Add(await SocialQueries.ToPostResponseAsync(
                _dbContext,
                _fileStorage,
                post,
                userId,
                cancellationToken));
        }

        var nextCursor = hasMore && items.Count > 0 ? items[^1].Id.ToString("D") : null;

        return Result<CursorPagedResult<PostResponse>>.Success(
            CursorPagedResult<PostResponse>.Create(items, nextCursor));
    }
}
