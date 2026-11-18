using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Organizations;

namespace Sportner.Application.Features.Organizations;

internal static class OrganizationQueries
{
    /// <summary>
    /// Silinmiş kulüpler için de çalışır: kulübe özel eski etkinliklerin görünürlüğü üyelik satırlarına bağlı.
    /// </summary>
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

    /// <summary>
    /// Üyelik satırını döndürür; kulüp silinmişse (soft delete) <c>null</c> döner, böylece
    /// yalnızca üyeliği yükleyen komutlar da silinmiş kulüpte başarısız olur.
    /// </summary>
    internal static Task<OrganizationMember?> FindMembershipAsync(
        IApplicationDbContext dbContext,
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken) =>
        dbContext.OrganizationMembers
            .FirstOrDefaultAsync(
                member =>
                    member.OrganizationId == organizationId
                    && member.UserId == userId
                    && dbContext.Organizations.Any(organization => organization.Id == member.OrganizationId),
                cancellationToken);

    /// <summary>Kulübün tamamlanmış etkinlik sayısı; yalnızca bir sayı olduğu için görünürlük (yalnızca üyeler) süzülmez.</summary>
    internal static Task<int> CountCompletedEventsAsync(
        IApplicationDbContext dbContext,
        Guid organizationId,
        CancellationToken cancellationToken) =>
        dbContext.Events.AsNoTracking()
            .CountAsync(
                @event => @event.OrganizationId == organizationId && @event.Status == EventStatus.Completed,
                cancellationToken);

    internal static async Task<string> AllocateInviteCodeAsync(
        IApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var code = Organization.NewInviteCode();
            // Silinmiş kulüplerin davet kodu da unique index'te yer tutar.
            var exists = await dbContext.Organizations.IgnoreQueryFilters()
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
        Guid? viewerUserId,
        CancellationToken cancellationToken)
    {
        var organization = await dbContext.Organizations.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == organizationId, cancellationToken);

        if (organization is null)
        {
            return null;
        }

        var membership = viewerUserId is { } viewerId
            ? await dbContext.OrganizationMembers.AsNoTracking()
                .FirstOrDefaultAsync(
                    member => member.OrganizationId == organizationId && member.UserId == viewerId,
                    cancellationToken)
            : null;

        if (membership is null
            || membership.Status is OrganizationMemberStatus.Left
                or OrganizationMemberStatus.Rejected
                or OrganizationMemberStatus.Blocked)
        {
            return await ToPublicDetailAsync(dbContext, organization, membership, viewerUserId, cancellationToken);
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
        var isApprovedFounder = membership.IsApproved && membership.Role is OrganizationRole.Founder;

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
            membership.CanCreateEvents(organization.EventCreationPolicy),
            membership.CanManageMembers,
            membership.CanManageMembers,
            membership.IsApproved && membership.Role is not OrganizationRole.Founder,
            canSeeInvite ? organization.InviteCode : null,
            approvedCount,
            pendingCount,
            blockedCount,
            organization.IsPublic,
            organization.ImageUrl,
            false,
            (short)organization.EventCreationPolicy,
            isApprovedFounder,
            isApprovedFounder,
            await CountCompletedEventsAsync(dbContext, organizationId, cancellationToken));
    }

    /// <summary>
    /// Üye olmayan (ya da Left/Rejected/Blocked) görüntüleyen için kulübün sınırlı detayı.
    /// Özel kulüp de görünür; yalnızca herkese açık kulübe doğrudan katılınabilir.
    /// </summary>
    private static async Task<OrganizationDetailResponse> ToPublicDetailAsync(
        IApplicationDbContext dbContext,
        Organization organization,
        OrganizationMember? membership,
        Guid? viewerUserId,
        CancellationToken cancellationToken)
    {
        var cityName = organization.CityId is null
            ? null
            : await dbContext.Cities.AsNoTracking()
                .Where(city => city.Id == organization.CityId)
                .Select(city => city.Name)
                .FirstOrDefaultAsync(cancellationToken);

        var approvedCount = await dbContext.OrganizationMembers.AsNoTracking()
            .CountAsync(
                member =>
                    member.OrganizationId == organization.Id
                    && member.Status == OrganizationMemberStatus.Approved,
                cancellationToken);

        var canJoin = organization.IsPublic
            && viewerUserId is not null
            && (membership is null
                || membership.Status is OrganizationMemberStatus.Left
                    or OrganizationMemberStatus.Rejected);

        return new OrganizationDetailResponse(
            organization.Id,
            organization.Name,
            organization.Description,
            organization.CityId,
            cityName,
            organization.FounderUserId,
            null,
            membership is null ? null : (short)membership.Status,
            false,
            false,
            false,
            false,
            false,
            null,
            approvedCount,
            0,
            0,
            organization.IsPublic,
            organization.ImageUrl,
            canJoin,
            (short)organization.EventCreationPolicy,
            CompletedEventCount: await CountCompletedEventsAsync(dbContext, organization.Id, cancellationToken));
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
