using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Abstractions.Recommendations;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Explore.ExplorePeople;

public sealed record ExplorePeopleQuery(
    Guid? SportId = null,
    string? City = null,
    int Limit = 20) : IQuery<IReadOnlyList<ExplorePersonItemResponse>>;

public sealed class ExplorePeopleQueryValidator : AbstractValidator<ExplorePeopleQuery>
{
    public ExplorePeopleQueryValidator()
    {
        RuleFor(query => query.Limit).InclusiveBetween(1, 50);
    }
}

internal sealed class ExplorePeopleQueryHandler
    : IQueryHandler<ExplorePeopleQuery, IReadOnlyList<ExplorePersonItemResponse>>
{
    private readonly IRecommendationService _recommendationService;
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ExplorePeopleQueryHandler(
        IRecommendationService recommendationService,
        IApplicationDbContext dbContext,
        ICurrentUser currentUser)
    {
        _recommendationService = recommendationService;
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<ExplorePersonItemResponse>>> Handle(
        ExplorePeopleQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } viewerId)
        {
            return Result<IReadOnlyList<ExplorePersonItemResponse>>.Failure(
                ExploreErrors.NotAuthenticated);
        }

        // Oversample when filters apply so ranking still has enough survivors.
        var fetchLimit = request.SportId is not null || !string.IsNullOrWhiteSpace(request.City)
            ? Math.Min(50, request.Limit * 3)
            : request.Limit;

        var scored = await _recommendationService.ScorePeopleAsync(
            viewerId,
            fetchLimit,
            cancellationToken);

        IEnumerable<Scored<RecommendedPerson>> filtered = scored;

        if (request.SportId is { } sportId)
        {
            var matchingUserIds = await _dbContext.UserSports.AsNoTracking()
                .Where(sport => sport.SportId == sportId)
                .Select(sport => sport.UserId)
                .ToListAsync(cancellationToken);
            var matchSet = matchingUserIds.ToHashSet();
            filtered = filtered.Where(entry => matchSet.Contains(entry.Item.UserId));
        }

        if (!string.IsNullOrWhiteSpace(request.City))
        {
            var city = request.City.Trim().ToLowerInvariant();
            filtered = filtered.Where(entry =>
                entry.Item.City is not null
                && entry.Item.City.Trim().ToLowerInvariant() == city);
        }

        var items = filtered
            .Take(request.Limit)
            .Select(entry => new ExplorePersonItemResponse(
                entry.Item.UserId,
                entry.Item.Username,
                entry.Item.FirstName,
                entry.Item.ProfileImageUrl,
                entry.Item.City,
                entry.Item.MutualFriendsCount,
                entry.Item.SharedSportsCount,
                entry.Item.SameCity,
                entry.Item.SharedSportNames))
            .ToList();

        return Result<IReadOnlyList<ExplorePersonItemResponse>>.Success(items);
    }
}
