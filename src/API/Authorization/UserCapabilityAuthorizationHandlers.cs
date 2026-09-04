using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Persistence;

namespace Sportner.API.Authorization;

public sealed class ActiveUserRequirement : IAuthorizationRequirement;

public sealed class CanCreateContentRequirement : IAuthorizationRequirement;

public sealed class ActiveUserAuthorizationHandler : AuthorizationHandler<ActiveUserRequirement>
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _dbContext;

    public ActiveUserAuthorizationHandler(ICurrentUser currentUser, IApplicationDbContext dbContext)
    {
        _currentUser = currentUser;
        _dbContext = dbContext;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ActiveUserRequirement requirement)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return;
        }

        var user = await _dbContext.Users.AsNoTracking()
            .Include(candidate => candidate.ExternalLogins)
            .FirstOrDefaultAsync(candidate => candidate.Id == userId);

        if (user is not null && user.CanAuthenticate())
        {
            context.Succeed(requirement);
        }
    }
}

public sealed class CanCreateContentAuthorizationHandler
    : AuthorizationHandler<CanCreateContentRequirement>
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _dbContext;

    public CanCreateContentAuthorizationHandler(
        ICurrentUser currentUser,
        IApplicationDbContext dbContext)
    {
        _currentUser = currentUser;
        _dbContext = dbContext;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CanCreateContentRequirement requirement)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return;
        }

        var user = await _dbContext.Users.AsNoTracking()
            .Include(candidate => candidate.ExternalLogins)
            .FirstOrDefaultAsync(candidate => candidate.Id == userId);

        if (user is not null && user.CanCreateContent())
        {
            context.Succeed(requirement);
        }
    }
}
