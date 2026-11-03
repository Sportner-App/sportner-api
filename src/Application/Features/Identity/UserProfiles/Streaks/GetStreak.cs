using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.UserProfiles.Streaks;

public sealed record GetStreakQuery(Guid UserId) : IQuery<StreakResponse>;

internal sealed class GetStreakQueryHandler : IQueryHandler<GetStreakQuery, StreakResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public GetStreakQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<StreakResponse>> Handle(
        GetStreakQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await _dbContext.UserProfiles.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.UserId == request.UserId, cancellationToken);

        if (profile is null)
        {
            return Result<StreakResponse>.Failure(ProfileErrors.NotFound);
        }

        if (await ProfileQueries.CheckViewAccessAsync(
                _dbContext, profile, _currentUser.UserId, cancellationToken)
            is { } accessError)
        {
            return Result<StreakResponse>.Failure(accessError);
        }

        return Result<StreakResponse>.Success(await StreakQueries.BuildAsync(
            _dbContext,
            request.UserId,
            profile.WeeklyGoal,
            _timeProvider.GetUtcNow(),
            cancellationToken));
    }
}
