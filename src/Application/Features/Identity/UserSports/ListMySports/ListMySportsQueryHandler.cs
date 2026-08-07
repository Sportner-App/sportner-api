using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.UserSports.ListMySports;

internal sealed class ListMySportsQueryHandler
    : IQueryHandler<ListMySportsQuery, IReadOnlyList<UserSportResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListMySportsQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<UserSportResponse>>> Handle(
        ListMySportsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<IReadOnlyList<UserSportResponse>>.Failure(UserSportErrors.NotAuthenticated);
        }

        var sports = await UserSportQueries.GetForUserAsync(_dbContext, userId, cancellationToken);

        return Result<IReadOnlyList<UserSportResponse>>.Success(sports);
    }
}
