using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Organizations.ListBlockedOrganizationMembers;

public sealed record ListBlockedOrganizationMembersQuery(Guid OrganizationId)
    : IQuery<IReadOnlyList<OrganizationMemberResponse>>;

internal sealed class ListBlockedOrganizationMembersQueryHandler
    : IQueryHandler<ListBlockedOrganizationMembersQuery, IReadOnlyList<OrganizationMemberResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListBlockedOrganizationMembersQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<OrganizationMemberResponse>>> Handle(
        ListBlockedOrganizationMembersQuery request,
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

        if (membership is null || !membership.CanManageMembers)
        {
            return Result<IReadOnlyList<OrganizationMemberResponse>>.Failure(
                OrganizationErrors.CannotManageMembers);
        }

        var items = await (
                from member in _dbContext.OrganizationMembers.AsNoTracking()
                join profile in _dbContext.UserProfiles.AsNoTracking()
                    on member.UserId equals profile.UserId into profiles
                from profile in profiles.DefaultIfEmpty()
                where member.OrganizationId == request.OrganizationId
                    && member.Status == OrganizationMemberStatus.Blocked
                orderby member.RespondedAt ?? member.UpdatedAt ?? member.CreatedAt
                select new OrganizationMemberResponse(
                    member.UserId,
                    profile != null ? profile.Username : null,
                    profile != null ? profile.FirstName : null,
                    profile != null ? profile.LastName : null,
                    profile != null ? profile.ProfileImageUrl : null,
                    (short)member.Role,
                    (short)member.Status,
                    member.CreatedAt,
                    member.RespondedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<OrganizationMemberResponse>>.Success(items);
    }
}
