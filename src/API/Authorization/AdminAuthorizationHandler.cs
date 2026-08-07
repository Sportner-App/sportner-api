using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Sportner.Application.Abstractions.Authentication;

namespace Sportner.API.Authorization;

public sealed class AdminRequirement : IAuthorizationRequirement;

public sealed class AdminAuthorizationHandler : AuthorizationHandler<AdminRequirement>
{
    private readonly ICurrentUser _currentUser;
    private readonly AuthorizationAllowListOptions _options;

    public AdminAuthorizationHandler(
        ICurrentUser currentUser,
        IOptions<AuthorizationAllowListOptions> options)
    {
        _currentUser = currentUser;
        _options = options.Value;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminRequirement requirement)
    {
        if (_currentUser.UserId is { } userId
            && _options.AdminUserIds.Contains(userId))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
