using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Users;

namespace Sportner.Application.Features.Identity.Profiles;

/// <summary>
/// Every profile update follows the same shape: resolve the caller, load their profile, mutate it
/// through the domain and return the refreshed representation.
/// </summary>
internal abstract class ProfileUpdateHandlerBase
{
    protected ProfileUpdateHandlerBase(
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

    protected async Task<Result<MyProfileResponse>> UpdateAsync(
        Func<Profile, DateTimeOffset, Result> mutate,
        CancellationToken cancellationToken)
    {
        if (CurrentUser.UserId is not { } userId)
        {
            return Result<MyProfileResponse>.Failure(ProfileErrors.NotAuthenticated);
        }

        var profile = await DbContext.Profiles
            .FirstOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken);

        if (profile is null)
        {
            return Result<MyProfileResponse>.Failure(ProfileErrors.NotFound);
        }

        var mutation = mutate(profile, TimeProvider.GetUtcNow());

        if (mutation.IsFailure)
        {
            return Result<MyProfileResponse>.Failure(mutation.Errors);
        }

        await DbContext.SaveChangesAsync(cancellationToken);

        var sports = await ProfileQueries.GetSportsAsync(DbContext, userId, cancellationToken);
        var statistics = await ProfileQueries.GetStatisticsAsync(DbContext, userId, cancellationToken);

        return Result<MyProfileResponse>.Success(
            ProfileQueries.ToMyProfileResponse(profile, sports, statistics));
    }
}
