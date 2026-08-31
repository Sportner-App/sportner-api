using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sportner.API.Authorization;
using Sportner.API.Common;
using Sportner.Application.Features.Social.Comments.CreateComment;
using Sportner.Application.Features.Social.Comments.CreateReply;
using Sportner.Application.Features.Social.Comments.DeleteComment;
using Sportner.Application.Features.Social.Comments.ListComments;
using Sportner.Application.Features.Social.Comments.ListReplies;
using Sportner.Application.Features.Social.Comments.UpdateComment;

namespace Sportner.API.Controllers;

[Authorize]
[Route("api/posts/{postId:guid}/comments")]
public sealed class CommentsController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        Guid postId,
        [FromQuery] string? before,
        [FromQuery] int limit = 30,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
            new ListCommentsQuery(postId, before, limit),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.CanCreateContent)]
    public async Task<IActionResult> Create(
        Guid postId,
        [FromBody] CreateCommentBody request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new CreateCommentCommand(postId, request.Content),
            cancellationToken);

        return result.ToActionResult(StatusCodes.Status201Created);
    }

    [HttpGet("{commentId:guid}/replies")]
    public async Task<IActionResult> ListReplies(
        Guid postId,
        Guid commentId,
        [FromQuery] string? before,
        [FromQuery] int limit = 30,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
            new ListRepliesQuery(postId, commentId, before, limit),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("{parentCommentId:guid}/replies")]
    [Authorize(Policy = AuthorizationPolicies.CanCreateContent)]
    public async Task<IActionResult> Reply(
        Guid postId,
        Guid parentCommentId,
        [FromBody] CreateCommentBody request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new CreateReplyCommand(postId, parentCommentId, request.Content),
            cancellationToken);

        return result.ToActionResult(StatusCodes.Status201Created);
    }

    public sealed record CreateCommentBody(string Content);
}

[Authorize]
[Route("api/comments")]
public sealed class CommentActionsController : ApiControllerBase
{
    [HttpPut("{commentId:guid}")]
    public async Task<IActionResult> Update(
        Guid commentId,
        [FromBody] UpdateCommentBody request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new UpdateCommentCommand(commentId, request.Content),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpDelete("{commentId:guid}")]
    public async Task<IActionResult> Delete(Guid commentId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new DeleteCommentCommand(commentId), cancellationToken);
        return result.ToActionResult(StatusCodes.Status204NoContent);
    }

    public sealed record UpdateCommentBody(string Content);
}
