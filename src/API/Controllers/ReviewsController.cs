using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sportner.Application.DTOs.Reviews;
using Sportner.Application.Services;

namespace Sportner.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController(IReviewService reviewService) : ControllerBase
{
    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewDto>> Create(
        [FromBody] CreateReviewDto dto,
        CancellationToken cancellationToken)
    {
        var result = await reviewService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(
            nameof(GetForUser),
            new { userId = result.ReviewedId },
            result);
    }

    [HttpGet("user/{userId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<ReviewDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ReviewDto>>> GetForUser(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await reviewService.GetForUserAsync(userId, cancellationToken);
        return Ok(result);
    }
}
