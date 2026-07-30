using Microsoft.AspNetCore.Mvc;
using Sportner.Application.DTOs.Sports;
using Sportner.Application.Services;

namespace Sportner.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SportsController(ISportService sportService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SportDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SportDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var result = await sportService.GetAllAsync(cancellationToken);
        return Ok(result);
    }
}
