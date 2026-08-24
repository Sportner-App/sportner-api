using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Models;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Events.ListMyParticipatingEvents;

public sealed record ListMyParticipatingEventsQuery(int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<EventListItemResponse>>;

internal sealed class ListMyParticipatingEventsQueryHandler
    : IQueryHandler<ListMyParticipatingEventsQuery, PagedResult<EventListItemResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListMyParticipatingEventsQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<EventListItemResponse>>> Handle(
        ListMyParticipatingEventsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<PagedResult<EventListItemResponse>>.Failure(EventErrors.NotAuthenticated);
        }

        var pagination = new PaginationRequest(request.Page, request.PageSize);

        var eventIds = _dbContext.EventParticipants.AsNoTracking()
            .Where(participant =>
                participant.UserId == userId
                && participant.Status != ParticipantStatus.Rejected
                && participant.Status != ParticipantStatus.Cancelled)
            .Select(participant => participant.EventId);

        var events = _dbContext.Events.AsNoTracking()
            .Where(@event => eventIds.Contains(@event.Id) && @event.OrganizerUserId != userId)
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
