using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.UserProfiles.UpdateLocation;

internal sealed class UpdateLocationCommandHandler
    : ProfileUpdateHandlerBase, ICommandHandler<UpdateLocationCommand, MyProfileResponse>
{
    public UpdateLocationCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
        : base(dbContext, currentUser, timeProvider)
    {
    }

    public async Task<Result<MyProfileResponse>> Handle(
        UpdateLocationCommand request,
        CancellationToken cancellationToken)
    {
        string? canonicalCity = null;

        if (!string.IsNullOrWhiteSpace(request.City))
        {
            var requestedCity = request.City.Trim();
            canonicalCity = await DbContext.Cities
                .Where(city => city.Name == requestedCity)
                .Select(city => city.Name)
                .FirstOrDefaultAsync(cancellationToken);

            if (canonicalCity is null)
            {
                return Result<MyProfileResponse>.Failure(ProfileErrors.InvalidCity);
            }
        }

        return await UpdateAsync(
            (profile, utcNow) =>
            {
                profile.UpdateLocation(canonicalCity, utcNow);
                return Result.Success();
            },
            cancellationToken);
    }
}
