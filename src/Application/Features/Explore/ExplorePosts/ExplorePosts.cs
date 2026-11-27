using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Abstractions.Storage;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Social;

namespace Sportner.Application.Features.Explore.ExplorePosts;

/// <summary>Keşfet akışı: en yeniden eskiye (CreatedAt desc, Id desc).
/// <paramref name="FriendsOnly"/> yalnızca kabul edilmiş arkadaşların gönderilerini döner
/// (oturum yoksa boş liste). <paramref name="Before"/> son görülen gönderinin id'sidir;
/// verilirse yalnızca ondan daha eski gönderiler döner. Geçersiz ya da bulunamayan id
/// <c>Post.InvalidCursor</c> üretir; boş değer yok sayılır. Yanıt düz dizidir, devamı olup
/// olmadığını istemci dönen öğe sayısının limite eşitliğinden anlar.</summary>
public sealed record ExplorePostsQuery(
    int Limit = 20,
    bool FriendsOnly = false,
    string? Before = null)
    : IQuery<IReadOnlyList<PostResponse>>;

public sealed class ExplorePostsQueryValidator : AbstractValidator<ExplorePostsQuery>
{
    public ExplorePostsQueryValidator()
    {
        RuleFor(query => query.Limit).InclusiveBetween(1, 50);
    }
}

internal sealed class ExplorePostsQueryHandler
    : IQueryHandler<ExplorePostsQuery, IReadOnlyList<PostResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IFileStorage _fileStorage;

    public ExplorePostsQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IFileStorage fileStorage)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
    }

    public async Task<Result<IReadOnlyList<PostResponse>>> Handle(
        ExplorePostsQuery request,
        CancellationToken cancellationToken)
    {
        var viewerId = _currentUser.UserId;

        // Arkadaş filtresi oturum gerektirir; anonim çağrıda gösterilecek bir şey yok.
        if (request.FriendsOnly && viewerId is null)
        {
            return Result<IReadOnlyList<PostResponse>>.Success([]);
        }

        var query = _dbContext.Posts.AsNoTracking()
            .Include(post => post.Media)
            .Where(post => !post.IsHidden);

        var hiddenIds = SocialQueries.HiddenUserIds(_dbContext, viewerId);
        query = query.Where(post => !hiddenIds.Contains(post.UserId));

        if (viewerId is { } id)
        {
            if (request.FriendsOnly)
            {
                var friendIds = SocialQueries.AcceptedFriendIds(_dbContext, id);
                query = query.Where(post => friendIds.Contains(post.UserId));
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Before))
        {
            if (!Guid.TryParse(request.Before, out var beforeId))
            {
                return Result<IReadOnlyList<PostResponse>>.Failure(PostErrors.InvalidCursor);
            }

            var cursor = await _dbContext.Posts.AsNoTracking()
                .Where(post => post.Id == beforeId)
                .Select(post => new { post.CreatedAt, post.Id })
                .FirstOrDefaultAsync(cancellationToken);

            if (cursor is null)
            {
                return Result<IReadOnlyList<PostResponse>>.Failure(PostErrors.InvalidCursor);
            }

            var cursorIdKey = cursor.Id.ToString("D");
            query = query.Where(post =>
                post.CreatedAt < cursor.CreatedAt
                || (post.CreatedAt == cursor.CreatedAt
                    && post.Id.ToString().CompareTo(cursorIdKey) < 0));
        }

        var posts = await query
            .OrderByDescending(post => post.CreatedAt)
            .ThenByDescending(post => post.Id)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);

        var items = await SocialQueries.ToPostResponsesAsync(
            _dbContext,
            _fileStorage,
            posts,
            viewerId,
            cancellationToken);

        return Result<IReadOnlyList<PostResponse>>.Success(items);
    }
}
