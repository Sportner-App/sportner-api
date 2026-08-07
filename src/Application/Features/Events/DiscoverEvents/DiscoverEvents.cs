using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Models;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Events.DiscoverEvents;

public sealed record DiscoverEventsQuery(
    Guid? SportId = null,
    string? City = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<EventListItemResponse>>;

internal sealed class DiscoverEventsQueryHandler
    : IQueryHandler<DiscoverEventsQuery, PagedResult<EventListItemResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public DiscoverEventsQueryHandler(IApplicationDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result<PagedResult<EventListItemResponse>>> Handle(
        DiscoverEventsQuery request,
        CancellationToken cancellationToken)
    {
        var pagination = new PaginationRequest(request.Page, request.PageSize);
        var utcNow = _timeProvider.GetUtcNow();
        var cityFilter = string.IsNullOrWhiteSpace(request.City)
            ? null
            : request.City.Trim();

        // City is not a first-class Event column; match against address for v1 discovery.
        var query = EventQueries.ProjectListItems(_dbContext)
            .Where(item =>
                (item.Status == (short)EventStatus.Published || item.Status == (short)EventStatus.Full)
                && item.EventDate > utcNow);

        if (request.SportId is not null)
        {
            query = query.Where(item => item.SportId == request.SportId);
        }

        if (cityFilter is not null)
        {
            var lowered = cityFilter.ToLowerInvariant();
            query = query.Where(item => item.Address.ToLower().Contains(lowered));
        }

        query = query.OrderBy(item => item.EventDate);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip(pagination.Skip)
            .Take(pagination.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        return Result<PagedResult<EventListItemResponse>>.Success(
            PagedResult<EventListItemResponse>.Create(
                items,
                pagination.NormalizedPage,
                pagination.NormalizedPageSize,
                total));
    }
}
