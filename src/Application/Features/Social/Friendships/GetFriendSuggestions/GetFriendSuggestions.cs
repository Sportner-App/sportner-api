using FluentValidation;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Recommendations;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Social.Friendships.GetFriendSuggestions;

public sealed record GetFriendSuggestionsQuery(int Limit = 20)
    : IQuery<IReadOnlyList<FriendSuggestionItemResponse>>;

public sealed class GetFriendSuggestionsQueryValidator : AbstractValidator<GetFriendSuggestionsQuery>
{
    public GetFriendSuggestionsQueryValidator()
    {
        RuleFor(query => query.Limit).InclusiveBetween(1, 50);
    }
}

internal sealed class GetFriendSuggestionsQueryHandler
    : IQueryHandler<GetFriendSuggestionsQuery, IReadOnlyList<FriendSuggestionItemResponse>>
{
    private readonly IRecommendationService _recommendationService;
    private readonly ICurrentUser _currentUser;

    public GetFriendSuggestionsQueryHandler(
        IRecommendationService recommendationService,
        ICurrentUser currentUser)
    {
        _recommendationService = recommendationService;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<FriendSuggestionItemResponse>>> Handle(
        GetFriendSuggestionsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } viewerId)
        {
            return Result<IReadOnlyList<FriendSuggestionItemResponse>>.Failure(
                FriendshipErrors.NotAuthenticated);
        }

        var scored = await _recommendationService.ScorePeopleAsync(
            viewerId,
            request.Limit,
            cancellationToken);

        var items = scored
            .Select(entry => new FriendSuggestionItemResponse(
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

        return Result<IReadOnlyList<FriendSuggestionItemResponse>>.Success(items);
    }
}
