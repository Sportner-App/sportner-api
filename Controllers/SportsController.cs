using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportnerApi.Data;
using SportnerApi.Dtos;

namespace SportnerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SportsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SportDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SportDto>>> GetSports(
        CancellationToken cancellationToken = default)
    {
        var sports = await db.Sports
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new SportDto(s.Id, s.Name, s.IconName, s.Category))
            .ToListAsync(cancellationToken);

        return Ok(sports);
    }
}
