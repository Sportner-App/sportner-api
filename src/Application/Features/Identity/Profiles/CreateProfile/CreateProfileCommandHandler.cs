using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Users;

namespace Sportner.Application.Features.Identity.Profiles.CreateProfile;

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
            .Include(candidate => candidate.Profile)
            .FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        if (user is null)
        {
            return Result<MyProfileResponse>.Failure(ProfileErrors.NotFound);
        }

        if (user.Profile is not null)
        {
            return Result<MyProfileResponse>.Failure(ProfileErrors.AlreadyExists);
        }

        var username = ProfileQueries.NormalizeUsername(request.Username);

        if (await _dbContext.Profiles.AnyAsync(
                profile => profile.Username == username,
                cancellationToken))
        {
            return Result<MyProfileResponse>.Failure(ProfileErrors.UsernameTaken);
        }

        var utcNow = _timeProvider.GetUtcNow();

        var newProfile = Profile.Create(
            user.Id,
            username,
            request.FirstName,
            utcNow,
            request.LastName,
            request.IsProfilePublic);

        newProfile.UpdateBio(request.Bio, utcNow);
        newProfile.UpdateLocation(request.City, utcNow);

        user.AttachProfile(newProfile);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var statistics = await ProfileQueries.GetStatisticsAsync(_dbContext, user.Id, cancellationToken);

        return Result<MyProfileResponse>.Success(
            ProfileQueries.ToMyProfileResponse(newProfile, [], statistics));
    }
}
