using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Models;
using Sportner.Application.Common.Results;
using Sportner.Application.Common.Search;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Organizations.ListPublicOrganizations;

/// <summary>
/// Kulüpleri listeler. Özel kulüpler de görünür (<c>IsPublic</c> yanıtta döner); özel olmak
/// yalnızca üyeliğe girişi davet koduyla kısıtlar.
/// </summary>
public sealed record ListPublicOrganizationsQuery(
    string? Search,
    Guid? CityId,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<OrganizationSummaryResponse>>;

public sealed class ListPublicOrganizationsQueryValidator : AbstractValidator<ListPublicOrganizationsQuery>
{
    private const int MaxPageSize = 50;

    public ListPublicOrganizationsQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, MaxPageSize);
    }
}

internal sealed class ListPublicOrganizationsQueryHandler
    : IQueryHandler<ListPublicOrganizationsQuery, PagedResult<OrganizationSummaryResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListPublicOrganizationsQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<OrganizationSummaryResponse>>> Handle(
        ListPublicOrganizationsQuery request,
        CancellationToken cancellationToken)
    {
        var pagination = new PaginationRequest(request.Page, request.PageSize);
        var viewerId = _currentUser.UserId;

        var organizations = _dbContext.Organizations.AsNoTracking();

        if (request.CityId is { } cityId)
        {
            organizations = organizations.Where(organization => organization.CityId == cityId);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var normalized = SearchText.Normalize(request.Search.Trim());
            organizations = organizations.Where(organization =>
                SearchText.Normalize(organization.Name).Contains(normalized));
        }

        var total = await organizations.CountAsync(cancellationToken);

        var items = await (
                from organization in organizations
                join city in _dbContext.Cities.AsNoTracking()
                    on organization.CityId equals city.Id into cities
                from city in cities.DefaultIfEmpty()
                join myMembership in _dbContext.OrganizationMembers.AsNoTracking()
                        .Where(member => viewerId != null && member.UserId == viewerId)
                    on organization.Id equals myMembership.OrganizationId into memberships
                from myMembership in memberships.DefaultIfEmpty()
                let approvedCount = _dbContext.OrganizationMembers.Count(other =>
                    other.OrganizationId == organization.Id
                    && other.Status == OrganizationMemberStatus.Approved)
                orderby approvedCount descending, organization.Name, organization.Id
                select new OrganizationSummaryResponse(
                    organization.Id,
                    organization.Name,
                    organization.Description,
                    organization.ImageUrl,
                    city != null ? city.Name : null,
                    approvedCount,
                    organization.IsPublic,
                    myMembership != null ? (short?)myMembership.Status : null))
            .Skip(pagination.Skip)
            .Take(pagination.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        return Result<PagedResult<OrganizationSummaryResponse>>.Success(
            PagedResult<OrganizationSummaryResponse>.Create(
                items,
                pagination.NormalizedPage,
                pagination.NormalizedPageSize,
                total));
    }
}
