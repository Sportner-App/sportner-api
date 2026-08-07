using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.SavedLocations.ListSavedLocations;

internal sealed class ListSavedLocationsQueryHandler
    : IQueryHandler<ListSavedLocationsQuery, IReadOnlyList<SavedLocationResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListSavedLocationsQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<SavedLocationResponse>>> Handle(
        ListSavedLocationsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<IReadOnlyList<SavedLocationResponse>>.Failure(
                SavedLocationErrors.NotAuthenticated);
        }

        var locations = await _dbContext.UserSavedLocations.AsNoTracking()
            .Where(location => location.UserId == userId)
            .OrderByDescending(location => location.IsDefault)
            .ThenBy(location => location.Title)
            .Select(location => new SavedLocationResponse(
                location.Id,
                location.Title,
                location.Latitude,
                location.Longitude,
                location.Address,
                location.City,
                location.District,
                location.IsDefault,
                location.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<SavedLocationResponse>>.Success(locations);
    }
}
