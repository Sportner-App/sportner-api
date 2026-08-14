using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sportner.API.Authorization;
using Sportner.API.Common;
using Sportner.Application.Features.Messaging.CreateDirectConversation;
using Sportner.Application.Features.Messaging.CreateGroupConversation;
using Sportner.Application.Features.Messaging.GetConversationById;
using Sportner.Application.Features.Messaging.InviteConversationMember;
using Sportner.Application.Features.Messaging.LeaveConversation;
using Sportner.Application.Features.Messaging.ListMyConversations;
using Sportner.Application.Features.Messaging.MarkConversationRead;
using Sportner.Application.Features.Messaging.MuteConversation;
using Sportner.Application.Features.Messaging.SearchMessages;
using Sportner.Application.Features.Messaging.SearchMyConversations;
using Sportner.Application.Features.Messaging.UnmuteConversation;

namespace Sportner.API.Controllers;

[Authorize]
[Route("api/conversations")]
public sealed class ConversationsController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListMine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] short? type = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
            new ListMyConversationsQuery(page, pageSize, type),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new SearchMyConversationsQuery(q, take), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{conversationId:guid}")]
    public async Task<IActionResult> GetById(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new GetConversationByIdQuery(conversationId),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpGet("{conversationId:guid}/messages/search")]
    public async Task<IActionResult> SearchMessages(
        Guid conversationId,
        [FromQuery] string q,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
            new SearchMessagesQuery(conversationId, q, take),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("direct")]
    [Authorize(Policy = AuthorizationPolicies.CanCreateContent)]
    public async Task<IActionResult> CreateDirect(
        [FromBody] CreateDirectRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new CreateDirectConversationCommand(request.OtherUserId),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("groups")]
    [Authorize(Policy = AuthorizationPolicies.CanCreateContent)]
    public async Task<IActionResult> CreateGroup(
        [FromBody] CreateGroupRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new CreateGroupConversationCommand(
                request.Title,
                request.MemberUserIds ?? []),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("{conversationId:guid}/members")]
    [Authorize(Policy = AuthorizationPolicies.CanCreateContent)]
    public async Task<IActionResult> InviteMember(
        Guid conversationId,
        [FromBody] InviteMemberRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new InviteConversationMemberCommand(conversationId, request.UserId),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("{conversationId:guid}/leave")]
    public async Task<IActionResult> Leave(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new LeaveConversationCommand(conversationId),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("{conversationId:guid}/read")]
    public async Task<IActionResult> MarkRead(
        Guid conversationId,
        [FromBody] MarkReadRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new MarkConversationReadCommand(conversationId, request.MessageId),
            cancellationToken);

        return result.ToActionResult(StatusCodes.Status204NoContent);
    }

    [HttpPost("{conversationId:guid}/mute")]
    public async Task<IActionResult> Mute(
        Guid conversationId,
        [FromBody] MuteRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new MuteConversationCommand(conversationId, request?.Until),
            cancellationToken);

        return result.ToActionResult(StatusCodes.Status204NoContent);
    }

    [HttpPost("{conversationId:guid}/unmute")]
    public async Task<IActionResult> Unmute(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new UnmuteConversationCommand(conversationId),
            cancellationToken);

        return result.ToActionResult(StatusCodes.Status204NoContent);
    }

    public sealed record CreateDirectRequest(Guid OtherUserId);

    public sealed record CreateGroupRequest(string Title, IReadOnlyList<Guid>? MemberUserIds);

    public sealed record InviteMemberRequest(Guid UserId);

    public sealed record MarkReadRequest(Guid MessageId);

    public sealed record MuteRequest(DateTimeOffset? Until);
}
