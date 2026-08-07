using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sportner.API.Common;
using Sportner.Application.Features.Identity.Profiles.CreateProfile;
using Sportner.Application.Features.Identity.Profiles.GetMyProfile;
using Sportner.Application.Features.Identity.Profiles.GetProfileByUsername;
using Sportner.Application.Features.Identity.Profiles.GetPublicProfile;
using Sportner.Application.Features.Identity.Profiles.UpdateAvatar;
using Sportner.Application.Features.Identity.Profiles.UpdateBio;
using Sportner.Application.Features.Identity.Profiles.UpdateDisplayName;
using Sportner.Application.Features.Identity.Profiles.UpdateIntroVideo;
using Sportner.Application.Features.Identity.Profiles.UpdateLocation;
using Sportner.Application.Features.Identity.Profiles.UpdatePersonalDetails;
using Sportner.Application.Features.Identity.Profiles.UpdateUsername;
using Sportner.Application.Features.Identity.Profiles.UpdateVisibility;

namespace Sportner.API.Controllers;

[Authorize]
public sealed class ProfilesController : ApiControllerBase
{
    [HttpPost("me")]
    public async Task<IActionResult> CreateMyProfile(
        [FromBody] CreateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateProfileCommand(
            request.Username,
            request.FirstName,
            request.LastName,
            request.Bio,
            request.City,
            request.IsProfilePublic);

        var result = await Sender.Send(command, cancellationToken);
        return result.ToActionResult(StatusCodes.Status201Created);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetMyProfileQuery(), cancellationToken);
        return result.ToActionResult();
    }

    [AllowAnonymous]
    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetPublicProfile(Guid userId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetPublicProfileQuery(userId), cancellationToken);
        return result.ToActionResult();
    }

    [AllowAnonymous]
    [HttpGet("by-username/{username}")]
    public async Task<IActionResult> GetProfileByUsername(
        string username,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetProfileByUsernameQuery(username), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("me/username")]
    public async Task<IActionResult> UpdateUsername(
        [FromBody] UpdateUsernameRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new UpdateUsernameCommand(request.Username),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPut("me/display-name")]
    public async Task<IActionResult> UpdateDisplayName(
        [FromBody] UpdateDisplayNameRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new UpdateDisplayNameCommand(request.FirstName, request.LastName),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPut("me/bio")]
    public async Task<IActionResult> UpdateBio(
        [FromBody] UpdateBioRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new UpdateBioCommand(request.Bio), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("me/location")]
    public async Task<IActionResult> UpdateLocation(
        [FromBody] UpdateLocationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new UpdateLocationCommand(request.City), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("me/personal-details")]
    public async Task<IActionResult> UpdatePersonalDetails(
        [FromBody] UpdatePersonalDetailsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new UpdatePersonalDetailsCommand(request.Gender, request.BirthDate),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPut("me/visibility")]
    public async Task<IActionResult> UpdateVisibility(
        [FromBody] UpdateVisibilityRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new UpdateVisibilityCommand(request.IsProfilePublic),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPut("me/avatar")]
    public async Task<IActionResult> UpdateAvatar(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        await using var content = file?.OpenReadStream();

        var result = await Sender.Send(
            new UpdateAvatarCommand(content, file?.ContentType, file?.FileName),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPut("me/intro-video")]
    public async Task<IActionResult> UpdateIntroVideo(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        await using var content = file?.OpenReadStream();

        var result = await Sender.Send(
            new UpdateIntroVideoCommand(content, file?.ContentType, file?.FileName),
            cancellationToken);

        return result.ToActionResult();
    }

    public sealed record CreateProfileRequest(
        string Username,
        string FirstName,
        string? LastName,
        string? Bio,
        string? City,
        bool IsProfilePublic = true);

    public sealed record UpdateUsernameRequest(string Username);

    public sealed record UpdateDisplayNameRequest(string FirstName, string? LastName);

    public sealed record UpdateBioRequest(string? Bio);

    public sealed record UpdateLocationRequest(string? City);

    public sealed record UpdatePersonalDetailsRequest(short? Gender, DateOnly? BirthDate);

    public sealed record UpdateVisibilityRequest(bool IsProfilePublic);
}
