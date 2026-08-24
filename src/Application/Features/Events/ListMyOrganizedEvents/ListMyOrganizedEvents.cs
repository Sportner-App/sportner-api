using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Models;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Events.ListMyOrganizedEvents;

public sealed record ListMyOrganizedEventsQuery(int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<EventListItemResponse>>;

internal sealed class ListMyOrganizedEventsQueryHandler
    : IQueryHandler<ListMyOrganizedEventsQuery, PagedResult<EventListItemResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListMyOrganizedEventsQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<EventListItemResponse>>> Handle(
        ListMyOrganizedEventsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<PagedResult<EventListItemResponse>>.Failure(EventErrors.NotAuthenticated);
        }

        var pagination = new PaginationRequest(request.Page, request.PageSize);

        var events = _dbContext.Events.AsNoTracking()
            .Where(@event => @event.OrganizerUserId == userId)
            .OrderByDescending(@event => @event.EventDate);

        var query = EventQueries.ProjectListItems(_dbContext, events);

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
