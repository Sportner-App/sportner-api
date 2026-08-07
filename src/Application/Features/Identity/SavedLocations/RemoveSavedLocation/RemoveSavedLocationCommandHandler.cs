using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.SavedLocations.RemoveSavedLocation;

internal sealed class RemoveSavedLocationCommandHandler
    : ICommandHandler<RemoveSavedLocationCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public RemoveSavedLocationCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(
        RemoveSavedLocationCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result.Failure(SavedLocationErrors.NotAuthenticated);
        }

        var user = await _dbContext.Users
            .Include(candidate => candidate.SavedLocations)
            .FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(SavedLocationErrors.UserNotFound);
        }

        if (user.SavedLocations.All(location => location.Id != request.LocationId))
        {
            return Result.Failure(SavedLocationErrors.NotFound);
        }

        user.RemoveSavedLocation(request.LocationId, _timeProvider.GetUtcNow());

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
