using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.Profiles.GetPublicProfile;

internal sealed class GetPublicProfileQueryHandler
    : IQueryHandler<GetPublicProfileQuery, PublicProfileResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetPublicProfileQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<PublicProfileResponse>> Handle(
        GetPublicProfileQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await _dbContext.Profiles.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.UserId == request.UserId, cancellationToken);

        return await ProfileQueries.BuildPublicProfileAsync(
            _dbContext,
            profile,
            _currentUser.UserId,
            cancellationToken);
    }
}
