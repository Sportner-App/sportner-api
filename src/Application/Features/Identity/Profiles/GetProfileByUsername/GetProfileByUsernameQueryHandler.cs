using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.Profiles.GetProfileByUsername;

internal sealed class GetProfileByUsernameQueryHandler
    : IQueryHandler<GetProfileByUsernameQuery, PublicProfileResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetProfileByUsernameQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<PublicProfileResponse>> Handle(
        GetProfileByUsernameQuery request,
        CancellationToken cancellationToken)
    {
        var username = ProfileQueries.NormalizeUsername(request.Username);

        var profile = await _dbContext.Profiles.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Username == username, cancellationToken);

        return await ProfileQueries.BuildPublicProfileAsync(
            _dbContext,
            profile,
            _currentUser.UserId,
            cancellationToken);
    }
}
