using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Organizations;

namespace Sportner.Application.Features.Organizations;

internal static class OrganizationQueries
{
    internal static async Task<bool> IsApprovedMemberAsync(
        IApplicationDbContext dbContext,
        Guid organizationId,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        if (userId is null)
        {
            return false;
        }

        return await dbContext.OrganizationMembers.AsNoTracking()
            .AnyAsync(
                member =>
                    member.OrganizationId == organizationId
                    && member.UserId == userId
                    && member.Status == OrganizationMemberStatus.Approved,
                cancellationToken);
    }

    internal static Task<OrganizationMember?> FindMembershipAsync(
        IApplicationDbContext dbContext,
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken) =>
        dbContext.OrganizationMembers
            .FirstOrDefaultAsync(
                member => member.OrganizationId == organizationId && member.UserId == userId,
                cancellationToken);

    internal static async Task<string> AllocateInviteCodeAsync(
        IApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var code = Organization.NewInviteCode();
            var exists = await dbContext.Organizations
                .AnyAsync(organization => organization.InviteCode == code, cancellationToken);

            if (!exists)
            {
                return code;
            }
        }

        throw new InvalidOperationException("Could not allocate a unique organization invite code.");
    }

    internal static async Task<OrganizationDetailResponse?> GetDetailAsync(
        IApplicationDbContext dbContext,
        Guid organizationId,
        Guid viewerUserId,
        CancellationToken cancellationToken)
    {
        var organization = await dbContext.Organizations.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == organizationId, cancellationToken);

        if (organization is null)
        {
            return null;
        }

        var membership = await dbContext.OrganizationMembers.AsNoTracking()
            .FirstOrDefaultAsync(
                member => member.OrganizationId == organizationId && member.UserId == viewerUserId,
                cancellationToken);

        if (membership is null
            || membership.Status is OrganizationMemberStatus.Left
                or OrganizationMemberStatus.Rejected
                or OrganizationMemberStatus.Blocked)
        {
            return null;
        }

        var cityName = organization.CityId is null
            ? null
            : await dbContext.Cities.AsNoTracking()
                .Where(city => city.Id == organization.CityId)
                .Select(city => city.Name)
                .FirstOrDefaultAsync(cancellationToken);

        var approvedCount = await dbContext.OrganizationMembers.AsNoTracking()
            .CountAsync(
                member =>
                    member.OrganizationId == organizationId
                    && member.Status == OrganizationMemberStatus.Approved,
                cancellationToken);

        var pendingCount = membership.CanManageMembers
            ? await dbContext.OrganizationMembers.AsNoTracking()
                .CountAsync(
                    member =>
                        member.OrganizationId == organizationId
                        && member.Status == OrganizationMemberStatus.Pending,
                    cancellationToken)
            : 0;

        var blockedCount = membership.CanManageMembers
            ? await dbContext.OrganizationMembers.AsNoTracking()
                .CountAsync(
                    member =>
                        member.OrganizationId == organizationId
                        && member.Status == OrganizationMemberStatus.Blocked,
                    cancellationToken)
            : 0;

        var canSeeInvite = membership.CanManageMembers;

        return new OrganizationDetailResponse(
            organization.Id,
            organization.Name,
            organization.Description,
            organization.CityId,
            cityName,
            organization.FounderUserId,
            (short)membership.Role,
            (short)membership.Status,
            membership.CanManageMembers,
            membership.CanCreateEvents,
            membership.IsApproved && membership.Role is OrganizationRole.Founder,
            membership.CanManageMembers,
            membership.IsApproved && membership.Role is not OrganizationRole.Founder,
            canSeeInvite ? organization.InviteCode : null,
            approvedCount,
            pendingCount,
            blockedCount);
    }

    internal static async Task<OrganizationMemberResponse> ToMemberResponseAsync(
        IApplicationDbContext dbContext,
        OrganizationMember membership,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.UserProfiles.AsNoTracking()
            .Where(candidate => candidate.UserId == membership.UserId)
            .Select(candidate => new
            {
                candidate.Username,
                candidate.FirstName,
                candidate.LastName,
                candidate.ProfileImageUrl
            })
            .FirstOrDefaultAsync(cancellationToken);

        return new OrganizationMemberResponse(
            membership.UserId,
            profile?.Username,
            profile?.FirstName,
            profile?.LastName,
            profile?.ProfileImageUrl,
            (short)membership.Role,
            (short)membership.Status,
            membership.CreatedAt,
            membership.RespondedAt);
    }
}
