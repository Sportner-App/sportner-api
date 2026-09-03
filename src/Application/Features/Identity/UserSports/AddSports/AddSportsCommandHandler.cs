using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Gamification;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Identity.UserSports.AddSports;

internal sealed class AddSportsCommandHandler
    : ICommandHandler<AddSportsCommand, IReadOnlyList<UserSportResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly IBadgeAwarder _badgeAwarder;

    public AddSportsCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        IBadgeAwarder badgeAwarder)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _badgeAwarder = badgeAwarder;
    }

    public async Task<Result<IReadOnlyList<UserSportResponse>>> Handle(
        AddSportsCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<IReadOnlyList<UserSportResponse>>.Failure(UserSportErrors.NotAuthenticated);
        }

        var requestedIds = request.Sports.Select(item => item.SportId).ToArray();
        var catalogSports = await _dbContext.Sports
            .Where(sport => requestedIds.Contains(sport.Id))
            .ToListAsync(cancellationToken);

        if (catalogSports.Count != requestedIds.Length)
        {
            return Result<IReadOnlyList<UserSportResponse>>.Failure(UserSportErrors.SportNotFound);
        }

        if (catalogSports.Any(sport => !sport.CanBeUsed()))
        {
            return Result<IReadOnlyList<UserSportResponse>>.Failure(UserSportErrors.SportInactive);
        }

        var user = await _dbContext.Users
            .Include(candidate => candidate.Sports)
            .FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        if (user is null)
        {
            return Result<IReadOnlyList<UserSportResponse>>.Failure(UserSportErrors.UserNotFound);
        }

        if (user.Sports.Any(existing => requestedIds.Contains(existing.SportId)))
        {
            return Result<IReadOnlyList<UserSportResponse>>.Failure(UserSportErrors.AlreadyAdded);
        }

        var utcNow = _timeProvider.GetUtcNow();
        foreach (var item in request.Sports)
        {
            var userSport = user.AddSport(
                item.SportId,
                (SkillLevel)item.SkillLevel,
                utcNow,
                item.IsPrimary);
            _dbContext.MarkAsAdded(userSport);
        }

        await _badgeAwarder.EvaluateAfterUserSportChangedAsync(userId, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var sports = await UserSportQueries.GetForUserAsync(_dbContext, userId, cancellationToken);
        return Result<IReadOnlyList<UserSportResponse>>.Success(sports);
    }
}
