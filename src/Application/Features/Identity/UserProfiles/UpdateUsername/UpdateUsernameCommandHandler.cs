using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.UserProfiles.UpdateUsername;

internal sealed class UpdateUsernameCommandHandler
    : ICommandHandler<UpdateUsernameCommand, MyProfileResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public UpdateUsernameCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<MyProfileResponse>> Handle(
        UpdateUsernameCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<MyProfileResponse>.Failure(ProfileErrors.NotAuthenticated);
        }

        var profile = await _dbContext.UserProfiles
            .FirstOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken);

        if (profile is null)
        {
            return Result<MyProfileResponse>.Failure(ProfileErrors.NotFound);
        }

        var username = ProfileQueries.NormalizeUsername(request.Username);
        var utcNow = _timeProvider.GetUtcNow();

        if (!string.Equals(profile.Username, username, StringComparison.Ordinal))
        {
            var cooldown = TimeSpan.FromDays(ProfileQueries.UsernameChangeCooldownDays);

            if (utcNow - profile.UsernameChangedAt < cooldown)
            {
                return Result<MyProfileResponse>.Failure(ProfileErrors.UsernameChangeTooSoon);
            }

            var isTaken = await _dbContext.UserProfiles.AnyAsync(
                candidate => candidate.Username == username && candidate.UserId != userId,
                cancellationToken);

            if (isTaken)
            {
                return Result<MyProfileResponse>.Failure(ProfileErrors.UsernameTaken);
            }
        }

        profile.UpdateUsername(username, utcNow);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var sports = await ProfileQueries.GetSportsAsync(_dbContext, userId, cancellationToken);
        var statistics = await ProfileQueries.GetStatisticsAsync(_dbContext, userId, cancellationToken);

        return Result<MyProfileResponse>.Success(
            ProfileQueries.ToMyProfileResponse(profile, sports, statistics));
    }
}
