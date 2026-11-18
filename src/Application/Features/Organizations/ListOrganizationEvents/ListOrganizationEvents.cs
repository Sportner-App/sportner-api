using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Social;
using Sportner.Application.Features.Events;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Organizations.ListOrganizationEvents;

/// <param name="Scope">
/// null / "upcoming" (varsayılan): yaklaşan etkinlikler, tarihe göre artan, sayfalamasız.
/// "past": tamamlanmış ya da başlangıcı geçmiş (iptal/taslak olmayan) etkinlikler, en yeni önce;
/// <paramref name="Page"/> (1 tabanlı) ve <paramref name="PageSize"/> (varsayılan 20, en çok 50) ile sayfalanır.
/// </param>
public sealed record ListOrganizationEventsQuery(
    Guid OrganizationId,
    string? Scope = null,
    int Page = 1,
    int PageSize = ListOrganizationEventsQuery.DefaultPageSize)
    : IQuery<IReadOnlyList<EventListItemResponse>>
{
    public const int DefaultPageSize = 20;

    public const int MaxPageSize = 50;

    /// <summary>OFFSET (page - 1) * pageSize taşmasın diye sayfa üst sınırı.</summary>
    public const int MaxPage = 1000;

    /// <summary>Yaklaşan etkinlik listesi sayfalanmadığı için sonuç sayısı sınırlı.</summary>
    public const int MaxUpcoming = 50;

    public const string UpcomingScope = "upcoming";

    public const string PastScope = "past";
}

public sealed class ListOrganizationEventsQueryValidator : AbstractValidator<ListOrganizationEventsQuery>
{
    public ListOrganizationEventsQueryValidator()
    {
        RuleFor(query => query.Scope)
            .Must(scope => scope is null
                || string.Equals(scope, ListOrganizationEventsQuery.UpcomingScope, StringComparison.OrdinalIgnoreCase)
                || string.Equals(scope, ListOrganizationEventsQuery.PastScope, StringComparison.OrdinalIgnoreCase))
            .WithMessage("Scope must be 'upcoming' or 'past'.");
        RuleFor(query => query.Page).InclusiveBetween(1, ListOrganizationEventsQuery.MaxPage);
        RuleFor(query => query.PageSize).InclusiveBetween(1, ListOrganizationEventsQuery.MaxPageSize);
    }
}

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
        var hiddenTestIds = BlockQueries.HiddenTestAccountIds(_dbContext, _currentUser.UserId);

        var exists = await _dbContext.Organizations.AsNoTracking()
            .AnyAsync(
                organization =>
                    organization.Id == request.OrganizationId
                    && !hiddenTestIds.Contains(organization.FounderUserId),
                cancellationToken);

        if (!exists)
        {
            return Result<IReadOnlyList<EventListItemResponse>>.Failure(OrganizationErrors.NotFound);
        }

        var isMember = await OrganizationQueries.IsApprovedMemberAsync(
            _dbContext,
            request.OrganizationId,
            _currentUser.UserId,
            cancellationToken);

        var utcNow = _timeProvider.GetUtcNow();
        // Taslak yalnızca yönetebilenlere görünür: kulüp yöneticisi ya da oluşturan.
        var viewerId = _currentUser.UserId;
        var isManager = viewerId is { } managerCandidate
            && await EventAccess.IsClubManagerAsync(
                _dbContext, request.OrganizationId, managerCandidate, cancellationToken);

        var visible = EventQueries.ExcludeTestHosted(_dbContext, _dbContext.Events.AsNoTracking(), viewerId)
            .Where(@event =>
                @event.OrganizationId == request.OrganizationId
                && (isMember || !@event.IsOrganizationOnly)
                && @event.Status != EventStatus.Cancelled
                && (@event.Status != EventStatus.Draft
                    || isManager
                    || (viewerId != null && @event.OrganizerUserId == viewerId)));

        var isPast = string.Equals(
            request.Scope, ListOrganizationEventsQuery.PastScope, StringComparison.OrdinalIgnoreCase);
        IQueryable<Domain.Events.Event> events;

        if (isPast)
        {
            events = visible
                .Where(@event =>
                    @event.Status == EventStatus.Completed
                    || (@event.Status != EventStatus.Draft && @event.EventDate <= utcNow))
                .OrderByDescending(@event => @event.EventDate)
                .ThenBy(@event => @event.Id)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize);
        }
        else
        {
            events = visible
                .Where(@event => @event.EventDate >= utcNow.AddDays(-1))
                .OrderBy(@event => @event.EventDate)
                .ThenBy(@event => @event.Id)
                .Take(ListOrganizationEventsQuery.MaxUpcoming);
        }

        var loaded = await EventQueries.ProjectListItems(_dbContext, events)
            .ToListAsync(cancellationToken);

        // Projeksiyondaki join sıralamayı garanti etmez ve EF DTO üyesine göre sıralamayı çeviremez;
        // sayfa küçük olduğundan (en çok 50) sıra bellekte kesin olarak kurulur.
        IReadOnlyList<EventListItemResponse> items = isPast
            ? loaded.OrderByDescending(item => item.EventDate).ThenBy(item => item.Id).ToList()
            : loaded.OrderBy(item => item.EventDate).ToList();

        return Result<IReadOnlyList<EventListItemResponse>>.Success(items);
    }
}
