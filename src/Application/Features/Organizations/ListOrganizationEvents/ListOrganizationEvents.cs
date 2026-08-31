using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Events;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Organizations.ListOrganizationEvents;

public sealed record ListOrganizationEventsQuery(Guid OrganizationId)
    : IQuery<IReadOnlyList<EventListItemResponse>>;

internal sealed class ListOrganizationEventsQueryHandler
    : IQueryHandler<ListOrganizationEventsQuery, IReadOnlyList<EventListItemResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public ListOrganizationEventsQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<IReadOnlyList<EventListItemResponse>>> Handle(
        ListOrganizationEventsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<IReadOnlyList<EventListItemResponse>>.Failure(
                OrganizationErrors.NotAuthenticated);
        }

        if (!await OrganizationQueries.IsApprovedMemberAsync(
                _dbContext,
                request.OrganizationId,
                userId,
                cancellationToken))
        {
            return Result<IReadOnlyList<EventListItemResponse>>.Failure(OrganizationErrors.NotFound);
        }

        var utcNow = _timeProvider.GetUtcNow();
        var items = await EventQueries.ProjectListItems(
                _dbContext,
                _dbContext.Events.AsNoTracking()
                    .Where(@event =>
                        @event.OrganizationId == request.OrganizationId
                        && @event.Status != EventStatus.Cancelled
                        && @event.EventDate >= utcNow.AddDays(-1)))
            .OrderBy(@event => @event.EventDate)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<EventListItemResponse>>.Success(items);
    }
}
