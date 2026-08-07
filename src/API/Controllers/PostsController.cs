using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sportner.API.Authorization;
using Sportner.API.Common;
using Sportner.Application.Features.Social.Posts.AddPostMedia;
using Sportner.Application.Features.Social.Posts.CreatePost;
using Sportner.Application.Features.Social.Posts.DeletePost;
using Sportner.Application.Features.Social.Posts.GetPostById;
using Sportner.Application.Features.Social.Posts.LikePost;
using Sportner.Application.Features.Social.Posts.ListPostsByUser;
using Sportner.Application.Features.Social.Posts.RemovePostMedia;
using Sportner.Application.Features.Social.Posts.ReorderPostMedia;
using Sportner.Application.Features.Social.Posts.UnlikePost;
using Sportner.Application.Features.Social.Posts.UpdatePostContent;

namespace Sportner.API.Controllers;

[Authorize]
[Route("api/posts")]
public sealed class PostsController : ApiControllerBase
{
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.CanCreateContent)]
    [RequestSizeLimit(100_000_000)]
    public async Task<IActionResult> Create(
        [FromForm] string? content,
        [FromForm] List<IFormFile>? files,
        CancellationToken cancellationToken)
    {
        var media = new List<CreatePostMediaInput>();

        if (files is not null)
        {
            foreach (var file in files.Where(item => item.Length > 0))
            {
                media.Add(new CreatePostMediaInput(
                    file.OpenReadStream(),
                    file.ContentType,
                    file.FileName,
                    file.Length));
            }
        }

        try
        {
            var result = await Sender.Send(
                new CreatePostCommand(content, media.Count == 0 ? null : media),
                cancellationToken);

            return result.ToActionResult(StatusCodes.Status201Created);
        }
        finally
        {
            foreach (var item in media)
            {
                await item.Content.DisposeAsync();
            }
        }
    }

    [HttpGet("{postId:guid}")]
    public async Task<IActionResult> GetById(Guid postId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetPostByIdQuery(postId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("{postId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.CanCreateContent)]
    public async Task<IActionResult> UpdateContent(
        Guid postId,
        [FromBody] UpdatePostContentBody request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new UpdatePostContentCommand(postId, request.Content),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("{postId:guid}/media")]
    [Authorize(Policy = AuthorizationPolicies.CanCreateContent)]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> AddMedia(
        Guid postId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length <= 0)
        {
            return BadRequest();
        }

        await using var stream = file.OpenReadStream();

        var result = await Sender.Send(
            new AddPostMediaCommand(postId, stream, file.ContentType, file.FileName, file.Length),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpDelete("{postId:guid}/media/{mediaId:guid}")]
    public async Task<IActionResult> RemoveMedia(
        Guid postId,
        Guid mediaId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new RemovePostMediaCommand(postId, mediaId),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPut("{postId:guid}/media/order")]
    public async Task<IActionResult> ReorderMedia(
        Guid postId,
        [FromBody] ReorderMediaBody request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new ReorderPostMediaCommand(postId, request.OrderedMediaIds),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpDelete("{postId:guid}")]
    public async Task<IActionResult> Delete(Guid postId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new DeletePostCommand(postId), cancellationToken);
        return result.ToActionResult(StatusCodes.Status204NoContent);
    }

    [HttpPost("{postId:guid}/likes")]
    public async Task<IActionResult> Like(Guid postId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new LikePostCommand(postId), cancellationToken);
        return result.ToActionResult(StatusCodes.Status204NoContent);
    }

    [HttpDelete("{postId:guid}/likes")]
    public async Task<IActionResult> Unlike(Guid postId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new UnlikePostCommand(postId), cancellationToken);
        return result.ToActionResult(StatusCodes.Status204NoContent);
    }

    public sealed record UpdatePostContentBody(string? Content);

    public sealed record ReorderMediaBody(IReadOnlyList<Guid> OrderedMediaIds);
}

[Authorize]
[Route("api/users")]
public sealed class UserPostsController : ApiControllerBase
{
    [HttpGet("{userId:guid}/posts")]
    public async Task<IActionResult> ListByUser(
        Guid userId,
        [FromQuery] string? before,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
            new ListPostsByUserQuery(userId, before, limit),
            cancellationToken);

        return result.ToActionResult();
    }
}
