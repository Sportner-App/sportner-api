using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sportner.API.Authorization;
using Sportner.API.Common;
using Sportner.API.Extensions.RateLimiting;
using Sportner.Application.Features.Identity.Auth.Login;
using Sportner.Application.Features.Identity.Auth.Logout;
using Sportner.Application.Features.Identity.Auth.LogoutAll;
using Sportner.Application.Features.Identity.Auth.RefreshToken;
using Sportner.Application.Features.Identity.Auth.Register;

namespace Sportner.API.Controllers;

public sealed class AuthController : ApiControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(
            request.Username,
            request.Password,
            request.FirstName,
            request.LastName,
            IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent: HttpContext.Request.Headers.UserAgent.ToString());

        var result = await Sender.Send(command, cancellationToken);
        return result.ToActionResult(StatusCodes.Status201Created);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var command = new LoginCommand(
            request.Username,
            request.Password,
            IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent: HttpContext.Request.Headers.UserAgent.ToString());

        var result = await Sender.Send(command, cancellationToken);
        return result.ToActionResult();
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
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

    public sealed record RegisterRequest(
        string Username,
        string Password,
        string FirstName,
        string? LastName);

    public sealed record LoginRequest(string Username, string Password);

    public sealed record RefreshTokenRequest(string RefreshToken);

    public sealed record LogoutRequest(string RefreshToken);
}
