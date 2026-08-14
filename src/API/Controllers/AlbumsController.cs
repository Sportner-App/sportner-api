using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sportner.API.Authorization;
using Sportner.API.Common;
using Sportner.Application.Features.Albums.AddAlbumMedia;
using Sportner.Application.Features.Albums.CreateProfileAlbum;
using Sportner.Application.Features.Albums.DeleteAlbum;
using Sportner.Application.Features.Albums.GetAlbumById;
using Sportner.Application.Features.Albums.ListMyAlbums;
using Sportner.Application.Features.Albums.ListUserAlbums;
using Sportner.Application.Features.Albums.RemoveAlbumMedia;
using Sportner.Application.Features.Albums.ReorderAlbumMedia;
using Sportner.Application.Features.Albums.SetAlbumCover;
using Sportner.Application.Features.Albums.UpdateAlbum;
using Sportner.Domain.Common.Enums;

namespace Sportner.API.Controllers;

[Authorize]
[Route("api/albums")]
public sealed class AlbumsController : ApiControllerBase
{
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.CanCreateContent)]
    public async Task<IActionResult> CreateProfile(
        [FromBody] CreateProfileAlbumBody request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new CreateProfileAlbumCommand(
                request.Title,
                request.Description,
                request.Visibility ?? (short)AlbumVisibility.Private),
            cancellationToken);
        return result.ToActionResult(StatusCodes.Status201Created);
    }

    [HttpGet("me")]
    public async Task<IActionResult> ListMine(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ListMyAlbumsQuery(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{albumId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid albumId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetAlbumByIdQuery(albumId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("{albumId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.CanCreateContent)]
    public async Task<IActionResult> Update(
        Guid albumId,
        [FromBody] UpdateAlbumBody request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new UpdateAlbumCommand(albumId, request.Title, request.Description, request.Visibility),
            cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{albumId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.CanCreateContent)]
    public async Task<IActionResult> Delete(Guid albumId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new DeleteAlbumCommand(albumId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{albumId:guid}/media")]
    [Authorize(Policy = AuthorizationPolicies.CanCreateContent)]
    [RequestSizeLimit(30_000_000)]
    public async Task<IActionResult> AddMedia(
        Guid albumId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length <= 0)
        {
            return BadRequest();
        }

        await using var stream = file.OpenReadStream();
        var result = await Sender.Send(
            new AddAlbumMediaCommand(
                albumId,
                stream,
                file.ContentType,
                file.FileName,
                file.Length),
            cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{albumId:guid}/media/{mediaId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.CanCreateContent)]
    public async Task<IActionResult> RemoveMedia(
        Guid albumId,
        Guid mediaId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new RemoveAlbumMediaCommand(albumId, mediaId),
            cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("{albumId:guid}/media/order")]
    [Authorize(Policy = AuthorizationPolicies.CanCreateContent)]
    public async Task<IActionResult> ReorderMedia(
        Guid albumId,
        [FromBody] ReorderAlbumMediaBody request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new ReorderAlbumMediaCommand(albumId, request.OrderedMediaIds),
            cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("{albumId:guid}/cover")]
    [Authorize(Policy = AuthorizationPolicies.CanCreateContent)]
    public async Task<IActionResult> SetCover(
        Guid albumId,
        [FromBody] SetAlbumCoverBody request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new SetAlbumCoverCommand(albumId, request.MediaId),
            cancellationToken);
        return result.ToActionResult();
    }
}

[Authorize]
[Route("api/users")]
public sealed class UserAlbumsController : ApiControllerBase
{
    [AllowAnonymous]
    [HttpGet("{userId:guid}/albums")]
    public async Task<IActionResult> ListForUser(Guid userId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ListUserAlbumsQuery(userId), cancellationToken);
        return result.ToActionResult();
    }
}

public sealed record CreateProfileAlbumBody(
    string Title,
    string? Description,
    short? Visibility);

public sealed record UpdateAlbumBody(
    string Title,
    string? Description,
    short Visibility);

public sealed record ReorderAlbumMediaBody(IReadOnlyList<Guid> OrderedMediaIds);

public sealed record SetAlbumCoverBody(Guid MediaId);
