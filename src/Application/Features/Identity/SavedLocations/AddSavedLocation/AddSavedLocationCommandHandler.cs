using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.SavedLocations.AddSavedLocation;

internal sealed class AddSavedLocationCommandHandler
    : ICommandHandler<AddSavedLocationCommand, SavedLocationResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public AddSavedLocationCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<SavedLocationResponse>> Handle(
        AddSavedLocationCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<SavedLocationResponse>.Failure(SavedLocationErrors.NotAuthenticated);
        }

        // The aggregate manages the single-default invariant, so its locations must be loaded.
        var user = await _dbContext.Users
            .Include(candidate => candidate.SavedLocations)
            .FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        if (user is null)
        {
            return Result<SavedLocationResponse>.Failure(SavedLocationErrors.UserNotFound);
        }

        var location = user.AddSavedLocation(
            request.Title,
            request.Latitude,
            request.Longitude,
            request.Address,
            _timeProvider.GetUtcNow(),
            request.City,
            request.District,
            request.IsDefault);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<SavedLocationResponse>.Success(location.ToResponse());
    }
}
