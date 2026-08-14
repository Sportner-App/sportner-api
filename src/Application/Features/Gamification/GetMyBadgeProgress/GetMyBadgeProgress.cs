using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Gamification.GetMyBadgeProgress;

public sealed record GetMyBadgeProgressQuery : IQuery<IReadOnlyList<BadgeProgressItemResponse>>;

internal sealed class GetMyBadgeProgressQueryHandler
    : IQueryHandler<GetMyBadgeProgressQuery, IReadOnlyList<BadgeProgressItemResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetMyBadgeProgressQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<BadgeProgressItemResponse>>> Handle(
        GetMyBadgeProgressQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<IReadOnlyList<BadgeProgressItemResponse>>.Failure(
                BadgeErrors.NotAuthenticated);
        }

        var items = await BadgeQueries.GetProgressAsync(_dbContext, userId, cancellationToken);
        return Result<IReadOnlyList<BadgeProgressItemResponse>>.Success(items);
    }
}
