using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sportner.API.Authorization;
using Sportner.API.Common;
using Sportner.API.Extensions.RateLimiting;
using Sportner.Application.Features.Feedback.SubmitAppFeedback;

namespace Sportner.API.Controllers;

[Authorize]
[Route("api/feedback")]
public sealed class FeedbackController : ApiControllerBase
{
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.CanCreateContent)]
    [EnableRateLimiting(RateLimitingExtensions.FeedbackPolicy)]
    public async Task<IActionResult> Submit(
        [FromBody] SubmitFeedbackBody request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new SubmitAppFeedbackCommand(request.Content),
            cancellationToken);

        return result.ToActionResult(StatusCodes.Status201Created);
    }

    public sealed record SubmitFeedbackBody(string Content);
}
