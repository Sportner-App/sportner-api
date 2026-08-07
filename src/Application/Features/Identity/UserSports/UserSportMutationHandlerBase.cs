using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Users;

namespace Sportner.Application.Features.Identity.UserSports;

/// <summary>
/// Shared flow for mutating an existing user sport: resolve the caller, load their sports,
/// ensure the target exists, apply the change through the aggregate and return the refreshed list.
/// </summary>
internal abstract class UserSportMutationHandlerBase
{
    protected UserSportMutationHandlerBase(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        DbContext = dbContext;
        CurrentUser = currentUser;
        TimeProvider = timeProvider;
    }

    protected IApplicationDbContext DbContext { get; }

    protected ICurrentUser CurrentUser { get; }

    protected TimeProvider TimeProvider { get; }

    protected async Task<Result<IReadOnlyList<UserSportResponse>>> MutateAsync(
        Guid sportId,
        Action<User, DateTimeOffset> mutate,
        CancellationToken cancellationToken)
    {
        if (CurrentUser.UserId is not { } userId)
        {
            return Result<IReadOnlyList<UserSportResponse>>.Failure(UserSportErrors.NotAuthenticated);
        }

        var user = await DbContext.Users
            .Include(candidate => candidate.Sports)
            .FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        if (user is null)
        {
            return Result<IReadOnlyList<UserSportResponse>>.Failure(UserSportErrors.UserNotFound);
        }

        if (user.Sports.All(userSport => userSport.SportId != sportId))
        {
            return Result<IReadOnlyList<UserSportResponse>>.Failure(UserSportErrors.NotFound);
        }

        mutate(user, TimeProvider.GetUtcNow());

        await DbContext.SaveChangesAsync(cancellationToken);

        var sports = await UserSportQueries.GetForUserAsync(DbContext, userId, cancellationToken);

        return Result<IReadOnlyList<UserSportResponse>>.Success(sports);
    }
}
