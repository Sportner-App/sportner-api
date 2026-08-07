using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.SavedLocations.UpdateSavedLocation;

internal sealed class UpdateSavedLocationCommandHandler
    : ICommandHandler<UpdateSavedLocationCommand, SavedLocationResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public UpdateSavedLocationCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<SavedLocationResponse>> Handle(
        UpdateSavedLocationCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<SavedLocationResponse>.Failure(SavedLocationErrors.NotAuthenticated);
        }

        var location = await _dbContext.UserSavedLocations
            .FirstOrDefaultAsync(
                candidate => candidate.Id == request.LocationId && candidate.UserId == userId,
                cancellationToken);

        if (location is null)
        {
            return Result<SavedLocationResponse>.Failure(SavedLocationErrors.NotFound);
        }

        var utcNow = _timeProvider.GetUtcNow();

        location.Rename(request.Title, utcNow);
        location.UpdateCoordinates(request.Latitude, request.Longitude, utcNow);
        location.UpdateAddress(request.Address, utcNow, request.City, request.District);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<SavedLocationResponse>.Success(location.ToResponse());
    }
}
