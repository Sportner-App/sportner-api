using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sportner.API.Authorization;
using Sportner.API.Common;
using Sportner.Application.Features.Social.Friendships.AcceptFriendRequest;
using Sportner.Application.Features.Social.Blocks.BlockUser;
using Sportner.Application.Features.Social.Friendships.GetFriendSuggestions;
using Sportner.Application.Features.Social.Friendships.GetMutualFriends;
using Sportner.Application.Features.Social.Friendships.ListFriends;
using Sportner.Application.Features.Social.Friendships.ListPendingRequests;
using Sportner.Application.Features.Social.Friendships.RejectFriendRequest;
using Sportner.Application.Features.Social.Friendships.RemoveFriendship;
using Sportner.Application.Features.Social.Friendships.SearchFriends;
using Sportner.Application.Features.Social.Friendships.SendFriendRequest;

namespace Sportner.API.Controllers;

[Authorize]
[Route("api/friendships")]
public sealed class FriendshipsController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListFriends(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new ListFriendsQuery(page, pageSize), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("pending")]
    public async Task<IActionResult> ListPending(
        [FromQuery] bool outgoing = false,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new ListPendingRequestsQuery(outgoing), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("suggestions")]
    public async Task<IActionResult> Suggestions(
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new GetFriendSuggestionsQuery(limit), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new SearchFriendsQuery(q, take), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("mutual/{userId:guid}")]
    public async Task<IActionResult> MutualFriends(
        Guid userId,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new GetMutualFriendsQuery(userId, take), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.CanCreateContent)]
    public async Task<IActionResult> SendRequest(
        [FromBody] SendFriendRequestBody request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new SendFriendRequestCommand(request.UserId),
            cancellationToken);

        return result.ToActionResult(StatusCodes.Status201Created);
    }

    [HttpPost("{friendshipId:guid}/accept")]
    public async Task<IActionResult> Accept(Guid friendshipId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new AcceptFriendRequestCommand(friendshipId),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("{friendshipId:guid}/reject")]
    public async Task<IActionResult> Reject(Guid friendshipId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new RejectFriendRequestCommand(friendshipId),
            cancellationToken);

        return result.ToActionResult(StatusCodes.Status204NoContent);
    }

    /// <summary>
    /// Deprecated alias for <c>POST /api/blocks</c>.
    /// </summary>
    [HttpPost("block")]
    public async Task<IActionResult> Block(
        [FromBody] BlockUserBody request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new BlockUserCommand(request.UserId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{friendshipId:guid}")]
    public async Task<IActionResult> Remove(Guid friendshipId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new RemoveFriendshipCommand(friendshipId),
            cancellationToken);

        return result.ToActionResult(StatusCodes.Status204NoContent);
    }

    public sealed record SendFriendRequestBody(Guid UserId);

    public sealed record BlockUserBody(Guid UserId);
}
