using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.UserProfiles.GetMyProfile;

internal sealed class GetMyProfileQueryHandler : IQueryHandler<GetMyProfileQuery, MyProfileResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetMyProfileQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<MyProfileResponse>> Handle(
        GetMyProfileQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<MyProfileResponse>.Failure(ProfileErrors.NotAuthenticated);
        }

        var profile = await _dbContext.UserProfiles.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken);

        if (profile is null)
        {
            return Result<MyProfileResponse>.Failure(ProfileErrors.NotFound);
        }

        var sports = await ProfileQueries.GetSportsAsync(_dbContext, userId, cancellationToken);
        var statistics = await ProfileQueries.GetStatisticsAsync(_dbContext, userId, cancellationToken);

        return Result<MyProfileResponse>.Success(
            ProfileQueries.ToMyProfileResponse(profile, sports, statistics));
    }
}
