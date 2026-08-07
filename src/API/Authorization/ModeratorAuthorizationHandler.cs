using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Sportner.Application.Abstractions.Authentication;

namespace Sportner.API.Authorization;

public sealed class ModeratorRequirement : IAuthorizationRequirement;

public sealed class ModeratorAuthorizationHandler : AuthorizationHandler<ModeratorRequirement>
{
    private readonly ICurrentUser _currentUser;
    private readonly AuthorizationAllowListOptions _options;

    public ModeratorAuthorizationHandler(
        ICurrentUser currentUser,
        IOptions<AuthorizationAllowListOptions> options)
    {
        _currentUser = currentUser;
        _options = options.Value;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ModeratorRequirement requirement)
    {
        if (_currentUser.UserId is { } userId
            && _options.ModeratorUserIds.Contains(userId))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
