using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Abstractions.Recommendations;
using Sportner.Application.Abstractions.Storage;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Social;

namespace Sportner.Application.Features.Explore.ExplorePosts;

public sealed record ExplorePostsQuery(int Limit = 20)
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
    private readonly IRecommendationService _recommendationService;
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IFileStorage _fileStorage;

    public ExplorePostsQueryHandler(
        IRecommendationService recommendationService,
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IFileStorage fileStorage)
    {
        _recommendationService = recommendationService;
        _dbContext = dbContext;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
    }

    public async Task<Result<IReadOnlyList<PostResponse>>> Handle(
        ExplorePostsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } viewerId)
        {
            return Result<IReadOnlyList<PostResponse>>.Failure(ExploreErrors.NotAuthenticated);
        }

        var scored = await _recommendationService.ScorePostsAsync(
            viewerId,
            request.Limit,
            cancellationToken);

        if (scored.Count == 0)
        {
            return Result<IReadOnlyList<PostResponse>>.Success([]);
        }

        var postIds = scored.Select(entry => entry.Item.PostId).ToList();
        var posts = await _dbContext.Posts.AsNoTracking()
            .Include(post => post.Media)
            .Where(post => postIds.Contains(post.Id))
            .ToListAsync(cancellationToken);

        var postsById = posts.ToDictionary(post => post.Id);
        var items = new List<PostResponse>(scored.Count);

        foreach (var entry in scored)
        {
            if (!postsById.TryGetValue(entry.Item.PostId, out var post))
            {
                continue;
            }

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
