using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sportner.API.Authorization;
using Sportner.API.Common;
using Sportner.Application.Features.Identity.Auth.Logout;
using Sportner.Application.Features.Identity.Auth.LogoutAll;
using Sportner.Application.Features.Identity.Auth.RefreshToken;
using Sportner.Application.Features.Identity.Auth.RequestOtp;
using Sportner.Application.Features.Identity.Auth.VerifyOtp;

namespace Sportner.API.Controllers;

public sealed class AuthController : ApiControllerBase
{
    [AllowAnonymous]
    [HttpPost("request-otp")]
    public async Task<IActionResult> RequestOtp(
        [FromBody] RequestOtpRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RequestOtpCommand(request.PhoneNumber), cancellationToken);
        return result.ToActionResult(StatusCodes.Status202Accepted);
    }

    [AllowAnonymous]
    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp(
        [FromBody] VerifyOtpRequest request,
        CancellationToken cancellationToken)
    {
        var command = new VerifyOtpCommand(
            request.PhoneNumber,
            request.Code,
            IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent: HttpContext.Request.Headers.UserAgent.ToString());

        var result = await Sender.Send(command, cancellationToken);
        return result.ToActionResult();
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new RefreshTokenCommand(request.RefreshToken),
            cancellationToken);

        return result.ToActionResult();
    }

    [Authorize(Policy = AuthorizationPolicies.Authenticated)]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new LogoutCommand(request.RefreshToken), cancellationToken);
        return result.ToActionResult(StatusCodes.Status204NoContent);
    }

    [Authorize(Policy = AuthorizationPolicies.Authenticated)]
    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new LogoutAllCommand(), cancellationToken);
        return result.ToActionResult(StatusCodes.Status204NoContent);
    }

    public sealed record RequestOtpRequest(string PhoneNumber);

    public sealed record VerifyOtpRequest(string PhoneNumber, string Code);

    public sealed record RefreshTokenRequest(string RefreshToken);

    public sealed record LogoutRequest(string RefreshToken);
}
