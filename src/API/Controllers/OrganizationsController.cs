using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sportner.API.Authorization;
using Sportner.API.Common;
using Sportner.Application.Features.Organizations.ApproveOrganizationMember;
using Sportner.Application.Features.Organizations.BlockOrganizationMember;
using Sportner.Application.Features.Organizations.ListBlockedOrganizationMembers;
using Sportner.Application.Features.Organizations.RemoveOrganizationMember;
using Sportner.Application.Features.Organizations.UnblockOrganizationMember;
using Sportner.Application.Features.Organizations.CreateOrganization;
using Sportner.Application.Features.Organizations.GetOrganizationById;
using Sportner.Application.Features.Organizations.JoinOrganization;
using Sportner.Application.Features.Organizations.LeaveOrganization;
using Sportner.Application.Features.Organizations.ListMyOrganizations;
using Sportner.Application.Features.Organizations.ListOrganizationEvents;
using Sportner.Application.Features.Organizations.ListOrganizationMembers;
using Sportner.Application.Features.Organizations.RejectOrganizationMember;
using Sportner.Application.Features.Organizations.RotateInviteCode;
using Sportner.Application.Features.Organizations.UpdateOrganization;
using Sportner.Application.Features.Organizations.UpdateOrganizationMemberRole;

namespace Sportner.API.Controllers;

[Authorize]
[Route("api/organizations")]
public sealed class OrganizationsController : ApiControllerBase
{
    [HttpGet("mine")]
    public async Task<IActionResult> ListMine(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ListMyOrganizationsQuery(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{organizationId:guid}")]
    public async Task<IActionResult> GetById(Guid organizationId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetOrganizationByIdQuery(organizationId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{organizationId:guid}/events")]
    public async Task<IActionResult> ListEvents(Guid organizationId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new ListOrganizationEventsQuery(organizationId),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpGet("{organizationId:guid}/members")]
    public async Task<IActionResult> ListMembers(Guid organizationId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new ListOrganizationMembersQuery(organizationId),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpGet("{organizationId:guid}/members/blocked")]
    public async Task<IActionResult> ListBlockedMembers(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new ListBlockedOrganizationMembersQuery(organizationId),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.CanCreateContent)]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrganizationBody request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new CreateOrganizationCommand(request.Name, request.Description, request.CityId),
            cancellationToken);

        return result.ToActionResult(StatusCodes.Status201Created);
    }

    [HttpPost("join")]
    [Authorize(Policy = AuthorizationPolicies.CanCreateContent)]
    public async Task<IActionResult> Join(
        [FromBody] JoinOrganizationBody request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new JoinOrganizationCommand(request.InviteCode),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPatch("{organizationId:guid}")]
    public async Task<IActionResult> Update(
        Guid organizationId,
        [FromBody] UpdateOrganizationBody request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new UpdateOrganizationCommand(
                organizationId,
                request.Name,
                request.Description,
                request.CityId),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("{organizationId:guid}/invite-code/rotate")]
    public async Task<IActionResult> RotateInviteCode(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RotateInviteCodeCommand(organizationId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{organizationId:guid}/members/{userId:guid}/approve")]
    public async Task<IActionResult> ApproveMember(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new ApproveOrganizationMemberCommand(organizationId, userId),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("{organizationId:guid}/members/{userId:guid}/reject")]
    public async Task<IActionResult> RejectMember(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new RejectOrganizationMemberCommand(organizationId, userId),
            cancellationToken);

        return result.ToActionResult(StatusCodes.Status204NoContent);
    }

    [HttpPatch("{organizationId:guid}/members/{userId:guid}/role")]
    public async Task<IActionResult> UpdateMemberRole(
        Guid organizationId,
        Guid userId,
        [FromBody] UpdateMemberRoleBody request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new UpdateOrganizationMemberRoleCommand(organizationId, userId, request.Role),
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("{organizationId:guid}/members/{userId:guid}/remove")]
    public async Task<IActionResult> RemoveMember(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new RemoveOrganizationMemberCommand(organizationId, userId),
            cancellationToken);

        return result.ToActionResult(StatusCodes.Status204NoContent);
    }

    [HttpPost("{organizationId:guid}/members/{userId:guid}/block")]
    public async Task<IActionResult> BlockMember(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new BlockOrganizationMemberCommand(organizationId, userId),
            cancellationToken);

        return result.ToActionResult(StatusCodes.Status204NoContent);
    }

    [HttpPost("{organizationId:guid}/members/{userId:guid}/unblock")]
    public async Task<IActionResult> UnblockMember(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new UnblockOrganizationMemberCommand(organizationId, userId),
            cancellationToken);

        return result.ToActionResult(StatusCodes.Status204NoContent);
    }

    [HttpPost("{organizationId:guid}/leave")]
    public async Task<IActionResult> Leave(Guid organizationId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new LeaveOrganizationCommand(organizationId), cancellationToken);
        return result.ToActionResult(StatusCodes.Status204NoContent);
    }

    public sealed record CreateOrganizationBody(string Name, string? Description, Guid? CityId);

    public sealed record JoinOrganizationBody(string InviteCode);

    public sealed record UpdateOrganizationBody(string Name, string? Description, Guid? CityId);

    public sealed record UpdateMemberRoleBody(short Role);
}
