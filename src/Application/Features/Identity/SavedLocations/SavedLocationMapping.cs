using Sportner.Domain.Users;

namespace Sportner.Application.Features.Identity.SavedLocations;

internal static class SavedLocationMapping
{
    internal static SavedLocationResponse ToResponse(this UserSavedLocation location) =>
        new(
            location.Id,
            location.Title,
            location.Latitude,
            location.Longitude,
            location.Address,
            location.City,
            location.District,
            location.IsDefault,
            location.CreatedAt);
}
