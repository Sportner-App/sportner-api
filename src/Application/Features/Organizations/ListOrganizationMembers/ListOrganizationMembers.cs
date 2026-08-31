using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Organizations.ListOrganizationMembers;

public sealed record ListOrganizationMembersQuery(Guid OrganizationId)
    : IQuery<IReadOnlyList<OrganizationMemberResponse>>;

internal sealed class ListOrganizationMembersQueryHandler
    : IQueryHandler<ListOrganizationMembersQuery, IReadOnlyList<OrganizationMemberResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListOrganizationMembersQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<OrganizationMemberResponse>>> Handle(
        ListOrganizationMembersQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<IReadOnlyList<OrganizationMemberResponse>>.Failure(
                OrganizationErrors.NotAuthenticated);
        }

        var membership = await OrganizationQueries.FindMembershipAsync(
            _dbContext,
            request.OrganizationId,
            userId,
            cancellationToken);

        if (membership is null || !membership.IsApproved)
        {
            return Result<IReadOnlyList<OrganizationMemberResponse>>.Failure(OrganizationErrors.NotFound);
        }

        var query =
            from member in _dbContext.OrganizationMembers.AsNoTracking()
            join profile in _dbContext.UserProfiles.AsNoTracking()
                on member.UserId equals profile.UserId into profiles
            from profile in profiles.DefaultIfEmpty()
            where member.OrganizationId == request.OrganizationId
            select new { member, profile };

        if (!membership.CanManageMembers)
        {
            query = query.Where(row => row.member.Status == OrganizationMemberStatus.Approved);
        }
        else
        {
            query = query.Where(row =>
                row.member.Status == OrganizationMemberStatus.Approved
                || row.member.Status == OrganizationMemberStatus.Pending);
        }

        var items = await query
            .OrderBy(row => row.member.Role)
            .ThenBy(row => row.member.CreatedAt)
            .Select(row => new OrganizationMemberResponse(
                row.member.UserId,
                row.profile != null ? row.profile.Username : null,
                row.profile != null ? row.profile.FirstName : null,
                row.profile != null ? row.profile.LastName : null,
                row.profile != null ? row.profile.ProfileImageUrl : null,
                (short)row.member.Role,
                (short)row.member.Status,
                row.member.CreatedAt,
                row.member.RespondedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<OrganizationMemberResponse>>.Success(items);
    }
}
