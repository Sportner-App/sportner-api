using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sportner.API.Common;
using Sportner.Application.Features.Identity.UserProfiles.DiscoverUsers;

namespace Sportner.API.Controllers;

[Authorize]
[Route("api/users")]
public sealed class UsersController : ApiControllerBase
{
    [HttpGet("discover")]
    public async Task<IActionResult> Discover(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] Guid? sportId = null,
        [FromQuery] string? city = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
            new DiscoverUsersQuery(page, pageSize, search, sportId, city),
            cancellationToken);
        return result.ToActionResult();
    }
}
