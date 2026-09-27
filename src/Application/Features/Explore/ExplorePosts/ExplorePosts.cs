using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Abstractions.Storage;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Social;

namespace Sportner.Application.Features.Explore.ExplorePosts;

/// <summary>Keşfet akışı: en yeniden eskiye. <paramref name="FriendsOnly"/> yalnızca
/// kabul edilmiş arkadaşların gönderilerini döner (oturum yoksa boş liste).</summary>
public sealed record ExplorePostsQuery(int Limit = 20, bool FriendsOnly = false)
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

        if (viewerId is { } id)
        {
            var blockedIds = SocialQueries.BlockedUserIds(_dbContext, id);
            query = query.Where(post => !blockedIds.Contains(post.UserId));

            if (request.FriendsOnly)
            {
                var friendIds = SocialQueries.AcceptedFriendIds(_dbContext, id);
                query = query.Where(post => friendIds.Contains(post.UserId));
            }
        }

        var posts = await query
            .OrderByDescending(post => post.CreatedAt)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);

        var items = new List<PostResponse>(posts.Count);
        foreach (var post in posts)
        {
            items.Add(await SocialQueries.ToPostResponseAsync(
                _dbContext,
                _fileStorage,
                post,
                viewerId,
                cancellationToken));
        }

        return Result<IReadOnlyList<PostResponse>>.Success(items);
    }
}
