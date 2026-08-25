using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Models;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Events.ListMyParticipatingEvents;

public sealed record ListMyParticipatingEventsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Scope = null)
    : IQuery<PagedResult<EventListItemResponse>>;

internal sealed class ListMyParticipatingEventsQueryHandler
    : IQueryHandler<ListMyParticipatingEventsQuery, PagedResult<EventListItemResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public ListMyParticipatingEventsQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
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
        var now = _timeProvider.GetUtcNow();
        var scope = request.Scope?.Trim().ToLowerInvariant();

        var eventIds = _dbContext.EventParticipants.AsNoTracking()
            .Where(participant =>
                participant.UserId == userId
                && participant.Status != ParticipantStatus.Rejected
                && participant.Status != ParticipantStatus.Cancelled)
            .Select(participant => participant.EventId);

        var events = _dbContext.Events.AsNoTracking()
            .Where(@event => eventIds.Contains(@event.Id) && @event.OrganizerUserId != userId);

        // Classify by start time (EventDate), not duration — EF translates this
        // reliably, and "etkinlik saati geçti" matches the card clock.
        events = scope switch
        {
            "upcoming" => events.Where(@event =>
                @event.Status != EventStatus.Completed
                && @event.Status != EventStatus.Cancelled
                && @event.EventDate > now),
            "past" => events.Where(@event =>
                @event.Status == EventStatus.Completed
                || @event.Status == EventStatus.Cancelled
                || @event.EventDate <= now),
            _ => events
        };

        events = scope == "upcoming"
            ? events.OrderBy(@event => @event.EventDate)
            : events.OrderByDescending(@event => @event.EventDate);

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
