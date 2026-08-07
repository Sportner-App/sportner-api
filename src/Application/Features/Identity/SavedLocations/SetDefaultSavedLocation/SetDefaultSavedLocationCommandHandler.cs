using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.SavedLocations.SetDefaultSavedLocation;

internal sealed class SetDefaultSavedLocationCommandHandler
    : ICommandHandler<SetDefaultSavedLocationCommand, IReadOnlyList<SavedLocationResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public SetDefaultSavedLocationCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<IReadOnlyList<SavedLocationResponse>>> Handle(
        SetDefaultSavedLocationCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<IReadOnlyList<SavedLocationResponse>>.Failure(
                SavedLocationErrors.NotAuthenticated);
        }

        // The aggregate clears the previous default, so all locations must be loaded.
        var user = await _dbContext.Users
            .Include(candidate => candidate.SavedLocations)
            .FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        if (user is null)
        {
            return Result<IReadOnlyList<SavedLocationResponse>>.Failure(
                SavedLocationErrors.UserNotFound);
        }

        if (user.SavedLocations.All(location => location.Id != request.LocationId))
        {
            return Result<IReadOnlyList<SavedLocationResponse>>.Failure(SavedLocationErrors.NotFound);
        }

        user.SetDefaultSavedLocation(request.LocationId, _timeProvider.GetUtcNow());

        await _dbContext.SaveChangesAsync(cancellationToken);

        var locations = user.SavedLocations
            .OrderByDescending(location => location.IsDefault)
            .ThenBy(location => location.Title)
            .Select(location => location.ToResponse())
            .ToList();

        return Result<IReadOnlyList<SavedLocationResponse>>.Success(locations);
    }
}
