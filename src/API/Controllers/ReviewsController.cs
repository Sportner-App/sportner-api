using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sportner.API.Authorization;
using Sportner.API.Common;
using Sportner.Application.Features.Reviews.CreateReview;
using Sportner.Application.Features.Reviews.GetReviewById;
using Sportner.Application.Features.Reviews.ListReviewablePeers;
using Sportner.Application.Features.Reviews.ListReviewsForEvent;
using Sportner.Application.Features.Reviews.ListReviewsForUser;
using Sportner.Application.Features.Reviews.UpdateReview;

namespace Sportner.API.Controllers;

[Authorize]
public sealed class ReviewsController : ApiControllerBase
{
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.CanCreateContent)]
    public async Task<IActionResult> Create(
        [FromBody] CreateReviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new CreateReviewCommand(
                request.EventId,
                request.ReviewedUserId,
                request.Rating,
                request.Comment),
            cancellationToken);

        return result.ToActionResult(StatusCodes.Status201Created);
    }

    [HttpPut("{reviewId:guid}")]
    public async Task<IActionResult> Update(
        Guid reviewId,
        [FromBody] UpdateReviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new UpdateReviewCommand(reviewId, request.Rating, request.Comment),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpGet("{reviewId:guid}")]
    public async Task<IActionResult> GetById(Guid reviewId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetReviewByIdQuery(reviewId), cancellationToken);
        return result.ToActionResult();
    }

    public sealed record CreateReviewRequest(
        Guid EventId,
        Guid ReviewedUserId,
        short Rating,
        string? Comment);

    public sealed record UpdateReviewRequest(short Rating, string? Comment);
}

[Authorize]
[Route("api/users")]
public sealed class UserReviewsController : ApiControllerBase
{
    [HttpGet("{userId:guid}/reviews")]
    public async Task<IActionResult> ListForUser(
        Guid userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
            new ListReviewsForUserQuery(userId, page, pageSize),
            cancellationToken);

        return result.ToActionResult();
    }
}
