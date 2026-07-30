using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Sportner.Domain.Abstractions;

namespace Sportner.API.Services;

public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid? UserId
    {
        get
        {
            var principal = httpContextAccessor.HttpContext?.User;
            if (principal is null)
            {
                return null;
            }

            var value =
                principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? principal.FindFirstValue("sub");

            return Guid.TryParse(value, out var userId) ? userId : null;
        }
    }

    public string? Email =>
        httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email)
        ?? httpContextAccessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Email);

    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}
