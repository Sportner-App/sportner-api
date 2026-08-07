using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sportner.API.Common;
using Sportner.Application.Features.Messaging.GetConversationById;
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
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
            new ListMyConversationsQuery(page, pageSize),
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
}
