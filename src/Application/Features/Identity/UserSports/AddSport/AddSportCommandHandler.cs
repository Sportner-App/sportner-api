using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Identity.UserSports.AddSport;

internal sealed class AddSportCommandHandler
    : ICommandHandler<AddSportCommand, IReadOnlyList<UserSportResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public AddSportCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<IReadOnlyList<UserSportResponse>>> Handle(
        AddSportCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<IReadOnlyList<UserSportResponse>>.Failure(UserSportErrors.NotAuthenticated);
        }

        var sport = await _dbContext.Sports
            .FirstOrDefaultAsync(candidate => candidate.Id == request.SportId, cancellationToken);

        if (sport is null)
        {
            return Result<IReadOnlyList<UserSportResponse>>.Failure(UserSportErrors.SportNotFound);
        }

        if (!sport.CanBeUsed())
        {
            return Result<IReadOnlyList<UserSportResponse>>.Failure(UserSportErrors.SportInactive);
        }

        // The aggregate guards against duplicates and manages the single-primary invariant.
        var user = await _dbContext.Users
            .Include(candidate => candidate.Sports)
            .FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        if (user is null)
        {
            return Result<IReadOnlyList<UserSportResponse>>.Failure(UserSportErrors.UserNotFound);
        }

        if (user.Sports.Any(userSport => userSport.SportId == request.SportId))
        {
            return Result<IReadOnlyList<UserSportResponse>>.Failure(UserSportErrors.AlreadyAdded);
        }

        user.AddSport(
            request.SportId,
            (SkillLevel)request.SkillLevel,
            _timeProvider.GetUtcNow(),
            request.IsPrimary);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var sports = await UserSportQueries.GetForUserAsync(_dbContext, userId, cancellationToken);

        return Result<IReadOnlyList<UserSportResponse>>.Success(sports);
    }
}
