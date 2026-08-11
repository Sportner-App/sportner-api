using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sportner.API.Common;
using Sportner.Application.Features.Identity.Onboarding.CompleteOnboarding;

namespace Sportner.API.Controllers;

[Authorize]
[Route("api/me/onboarding")]
public sealed class OnboardingController : ApiControllerBase
{
    [HttpPost("complete")]
    public async Task<IActionResult> Complete(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new CompleteOnboardingCommand(), cancellationToken);
        return result.ToActionResult(StatusCodes.Status204NoContent);
    }
}
