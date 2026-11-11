using Microsoft.Extensions.Options;
using Sportner.Application.Abstractions.Authentication;

namespace Sportner.API.Authorization;

/// <inheritdoc />
public sealed class AdminDirectory : IAdminDirectory
{
    private readonly IOptionsMonitor<AuthorizationAllowListOptions> _options;

    public AdminDirectory(IOptionsMonitor<AuthorizationAllowListOptions> options) => _options = options;

    public bool IsAdmin(Guid userId) => _options.CurrentValue.AdminUserIds.Contains(userId);
}
