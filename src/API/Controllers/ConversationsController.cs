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

    public sealed record CreateDirectRequest(Guid OtherUserId);

    public sealed record CreateGroupRequest(string Title, IReadOnlyList<Guid>? MemberUserIds);

    public sealed record InviteMemberRequest(Guid UserId);
}
