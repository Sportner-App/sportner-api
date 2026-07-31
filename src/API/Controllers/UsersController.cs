using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sportner.Application.DTOs.Users;
using Sportner.Application.Services;
using Sportner.Domain.Abstractions;
using Sportner.Domain.Exceptions;
using Sportner.Localization.Resources;

namespace Sportner.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UsersController(
    IUserService userService,
    ICurrentUser currentUser) : ControllerBase
{
    private const long MaxAvatarBytes = 5 * 1024 * 1024;

    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetMe(CancellationToken cancellationToken)
    {
        var result = await userService.GetMeAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPut("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> UpdateMe(
        [FromBody] UpdateUserDto dto,
        CancellationToken cancellationToken)
    {
        var result = await userService.UpdateMeAsync(dto, cancellationToken);
        return Ok(result);
    }

    [HttpPost("me/avatar")]
    [RequestSizeLimit(MaxAvatarBytes)]
    [ProducesResponseType(typeof(AvatarUploadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    public async Task<ActionResult<AvatarUploadResponseDto>> UploadAvatar(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        var contentTypeHeader = Request.ContentType ?? string.Empty;
        if (!contentTypeHeader.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
        {
            throw new ApiException(
                HttpStatusCode.UnsupportedMediaType,
                ValidationResource.Exception_Profile_AvatarContentType);
        }

        if (file is null || file.Length == 0)
        {
            throw new ApiException(
                HttpStatusCode.BadRequest,
                ValidationResource.Exception_Profile_AvatarRequired);
        }

        var contentType = file.ContentType;
        var extension = contentType.ToLowerInvariant() switch
        {
            "image/png" => "png",
            "image/webp" => "webp",
            _ => "jpg"
        };

        await using var stream = file.OpenReadStream();
        var result = await userService.UploadAvatarAsync(
            currentUser.UserId!.Value,
            stream,
            contentType,
            extension,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetById(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await userService.GetByIdAsync(userId, cancellationToken);
        return Ok(result);
    }
}
