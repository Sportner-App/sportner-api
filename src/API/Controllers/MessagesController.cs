using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sportner.API.Authorization;
using Sportner.API.Common;
using Sportner.Application.Features.Messaging.EditMessage;
using Sportner.Application.Features.Messaging.ListMessages;
using Sportner.Application.Features.Messaging.RedactMessage;
using Sportner.Application.Features.Messaging.SendMediaMessage;
using Sportner.Application.Features.Messaging.SendTextMessage;

namespace Sportner.API.Controllers;

[Authorize]
[Route("api/conversations/{conversationId:guid}/messages")]
public sealed class MessagesController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        Guid conversationId,
        [FromQuery] string? before,
        [FromQuery] int limit = 30,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
            new ListMessagesQuery(conversationId, before, limit),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.CanCreateContent)]
    public async Task<IActionResult> SendText(
        Guid conversationId,
        [FromBody] SendTextRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new SendTextMessageCommand(conversationId, request.Content, request.ReplyToMessageId),
            cancellationToken);

        return result.ToActionResult(StatusCodes.Status201Created);
    }

    [HttpPost("media")]
    [Authorize(Policy = AuthorizationPolicies.CanCreateContent)]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> SendMedia(
        Guid conversationId,
        IFormFile file,
        [FromForm] string? caption,
        [FromForm] Guid? replyToMessageId,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length <= 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation failed",
                Detail = "A media file is required.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        await using var stream = file.OpenReadStream();

        var result = await Sender.Send(
            new SendMediaMessageCommand(
                conversationId,
                stream,
                file.ContentType,
                file.FileName,
                caption,
                replyToMessageId),
            cancellationToken);

        return result.ToActionResult(StatusCodes.Status201Created);
    }

    [HttpPut("{messageId:guid}")]
    public async Task<IActionResult> Edit(
        Guid conversationId,
        Guid messageId,
        [FromBody] EditMessageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new EditMessageCommand(conversationId, messageId, request.Content),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpDelete("{messageId:guid}")]
    public async Task<IActionResult> Redact(
        Guid conversationId,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new RedactMessageCommand(conversationId, messageId),
            cancellationToken);

        return result.ToActionResult();
    }

    public sealed record SendTextRequest(string Content, Guid? ReplyToMessageId = null);

    public sealed record EditMessageRequest(string Content);
}
