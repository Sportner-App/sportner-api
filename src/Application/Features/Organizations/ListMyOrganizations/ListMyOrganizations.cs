using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Organizations.ListMyOrganizations;

public sealed record ListMyOrganizationsQuery : IQuery<IReadOnlyList<OrganizationListItemResponse>>;

internal sealed class ListMyOrganizationsQueryHandler
    : IQueryHandler<ListMyOrganizationsQuery, IReadOnlyList<OrganizationListItemResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListMyOrganizationsQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<OrganizationListItemResponse>>> Handle(
        ListMyOrganizationsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<IReadOnlyList<OrganizationListItemResponse>>.Failure(
                OrganizationErrors.NotAuthenticated);
        }

        var items = await (
                from member in _dbContext.OrganizationMembers.AsNoTracking()
                join organization in _dbContext.Organizations.AsNoTracking()
                    on member.OrganizationId equals organization.Id
                join city in _dbContext.Cities.AsNoTracking()
                    on organization.CityId equals city.Id into cities
                from city in cities.DefaultIfEmpty()
                where member.UserId == userId
                    && (member.Status == OrganizationMemberStatus.Approved
                        || member.Status == OrganizationMemberStatus.Pending)
                orderby member.Status, organization.Name
                select new OrganizationListItemResponse(
                    organization.Id,
                    organization.Name,
                    city != null ? city.Name : null,
                    (short)member.Role,
                    (short)member.Status,
                    _dbContext.OrganizationMembers.Count(other =>
                        other.OrganizationId == organization.Id
                        && other.Status == OrganizationMemberStatus.Approved)))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<OrganizationListItemResponse>>.Success(items);
    }
}
