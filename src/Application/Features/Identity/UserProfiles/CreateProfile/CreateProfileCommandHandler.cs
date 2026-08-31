using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Users;

namespace Sportner.Application.Features.Identity.UserProfiles.CreateProfile;

internal sealed class CreateProfileCommandHandler
    : ICommandHandler<CreateProfileCommand, MyProfileResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public CreateProfileCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<MyProfileResponse>> Handle(
        CreateProfileCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<MyProfileResponse>.Failure(ProfileErrors.NotAuthenticated);
        }

        var user = await _dbContext.Users
            .Include(candidate => candidate.UserProfile)
            .FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        if (user is null)
        {
            return Result<MyProfileResponse>.Failure(ProfileErrors.NotFound);
        }

        if (user.UserProfile is not null)
        {
            return Result<MyProfileResponse>.Failure(ProfileErrors.AlreadyExists);
        }

        var username = ProfileQueries.NormalizeUsername(request.Username);

        if (await _dbContext.UserProfiles.AnyAsync(
                profile => profile.Username == username,
                cancellationToken))
        {
            return Result<MyProfileResponse>.Failure(ProfileErrors.UsernameTaken);
        }

        var utcNow = _timeProvider.GetUtcNow();
        string? canonicalCity = null;

        if (!string.IsNullOrWhiteSpace(request.City))
        {
            var requestedCity = request.City.Trim();
            canonicalCity = await _dbContext.Cities
                .Where(city => city.Name == requestedCity)
                .Select(city => city.Name)
                .FirstOrDefaultAsync(cancellationToken);

            if (canonicalCity is null)
            {
                return Result<MyProfileResponse>.Failure(ProfileErrors.InvalidCity);
            }
        }

        var newProfile = UserProfile.Create(
            user.Id,
            username,
            request.FirstName,
            utcNow,
            request.LastName,
            request.IsProfilePublic);

        newProfile.UpdateBio(request.Bio, utcNow);
        newProfile.UpdateLocation(canonicalCity, utcNow);

        user.AttachUserProfile(newProfile);

        // Client-generated Guids can be tracked as Modified by EF; force insert for new rows.
        _dbContext.MarkAsAdded(newProfile);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var statistics = await ProfileQueries.GetStatisticsAsync(_dbContext, user.Id, cancellationToken);

        return Result<MyProfileResponse>.Success(
            ProfileQueries.ToMyProfileResponse(newProfile, [], statistics));
    }
}
