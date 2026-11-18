using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Models;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Admin.Events;

public sealed record AdminClubRef(Guid Id, string Name);

public sealed record AdminEventListItem(
    Guid Id,
    string Title,
    short Status,
    DateTimeOffset EventDate,
    DateTimeOffset CreatedAt,
    AdminUserRef? Organizer,
    AdminClubRef? Club,
    string? SportName,
    string? CityName,
    int ParticipantCount,
    int? MaxParticipants,
    bool IsTestHosted);

public sealed record AdminEventParticipant(AdminUserRef? User, string? GuestName, short Status, DateTimeOffset? JoinedAt);

public sealed record AdminEventDetail(
    Guid Id,
    string Title,
    short Status,
    DateTimeOffset EventDate,
    DateTimeOffset CreatedAt,
    AdminUserRef? Organizer,
    AdminClubRef? Club,
    string? SportName,
    string? CityName,
    int ParticipantCount,
    int? MaxParticipants,
    bool IsTestHosted,
    string? Description,
    string? District,
    string? Address,
    bool IsPaid,
    decimal? FeeAmount,
    bool IsOrganizationOnly,
    int OpenReports,
    int TotalReports,
    IReadOnlyList<AdminEventParticipant> Participants);

internal sealed record AdminEventRow(
    Guid Id,
    string Title,
    EventStatus Status,
    DateTimeOffset EventDate,
    DateTimeOffset CreatedAt,
    Guid OrganizerUserId,
    Guid? OrganizationId,
    Guid SportId,
    string? City,
    int? MaxParticipants);

internal static class AdminEventMapper
{
    internal static async Task<List<AdminEventListItem>> BuildAsync(
        IApplicationDbContext dbContext,
        IReadOnlyList<AdminEventRow> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return [];
        }

        var eventIds = rows.Select(row => row.Id).ToList();
        var organizerIds = rows.Select(row => row.OrganizerUserId).Distinct().ToList();
        var clubIds = rows.Where(row => row.OrganizationId != null).Select(row => row.OrganizationId!.Value).Distinct().ToList();
        var sportIds = rows.Select(row => row.SportId).Distinct().ToList();

        var organizers = await AdminTargets.LoadUserRefsAsync(dbContext, organizerIds, cancellationToken);
        var testIds = (await dbContext.Users.AsNoTracking()
                .Where(user => organizerIds.Contains(user.Id) && user.IsTestAccount)
                .Select(user => user.Id)
                .ToListAsync(cancellationToken))
            .ToHashSet();
        var clubs = clubIds.Count == 0
            ? []
            : await dbContext.Organizations.AsNoTracking().IgnoreQueryFilters()
                .Where(club => clubIds.Contains(club.Id))
                .Select(club => new { club.Id, club.Name })
                .ToDictionaryAsync(club => club.Id, club => club.Name, cancellationToken);
        var sports = await dbContext.Sports.AsNoTracking()
            .Where(sport => sportIds.Contains(sport.Id))
            .Select(sport => new { sport.Id, sport.Name })
            .ToDictionaryAsync(sport => sport.Id, sport => sport.Name, cancellationToken);
        var counts = await dbContext.EventParticipants.AsNoTracking()
            .Where(participant => eventIds.Contains(participant.EventId)
                && (participant.Status == ParticipantStatus.Approved
                    || participant.Status == ParticipantStatus.Attended
                    || participant.Status == ParticipantStatus.NoShow))
            .GroupBy(participant => participant.EventId)
            .Select(group => new { EventId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.EventId, group => group.Count, cancellationToken);

        return rows.Select(row => new AdminEventListItem(
                row.Id,
                row.Title,
                (short)row.Status,
                row.EventDate,
                row.CreatedAt,
                organizers.GetValueOrDefault(row.OrganizerUserId),
                row.OrganizationId is { } clubId && clubs.TryGetValue(clubId, out var clubName)
                    ? new AdminClubRef(clubId, clubName)
                    : null,
                sports.GetValueOrDefault(row.SportId),
                row.City,
                counts.GetValueOrDefault(row.Id),
                row.MaxParticipants,
                testIds.Contains(row.OrganizerUserId)))
            .ToList();
    }
}

public sealed record ListAdminEventsQuery(string? Search, short? Status, int? Page, int? PageSize)
    : IQuery<PagedResult<AdminEventListItem>>;

internal sealed class ListAdminEventsQueryHandler
    : IQueryHandler<ListAdminEventsQuery, PagedResult<AdminEventListItem>>
{
    private readonly IApplicationDbContext _dbContext;

    public ListAdminEventsQueryHandler(IApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<PagedResult<AdminEventListItem>>> Handle(
        ListAdminEventsQuery request,
        CancellationToken cancellationToken)
    {
        var (page, pageSize) = AdminPaging.Normalize(request.Page, request.PageSize);
        var query = _dbContext.Events.AsNoTracking();

        if (request.Status is { } status)
        {
            var eventStatus = (EventStatus)status;
            query = query.Where(@event => @event.Status == eventStatus);
        }

        var search = request.Search?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(search))
        {
            query = Guid.TryParse(search, out var searchId)
                ? query.Where(@event => @event.Id == searchId)
                : query.Where(@event => @event.Title.ToLower().Contains(search));
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(@event => @event.EventDate)
            .ThenBy(@event => @event.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(@event => new AdminEventRow(
                @event.Id,
                @event.Title,
                @event.Status,
                @event.EventDate,
                @event.CreatedAt,
                @event.OrganizerUserId,
                @event.OrganizationId,
                @event.SportId,
                @event.City,
                @event.MaxParticipants))
            .ToListAsync(cancellationToken);

        var items = await AdminEventMapper.BuildAsync(_dbContext, rows, cancellationToken);
        return Result<PagedResult<AdminEventListItem>>.Success(
            PagedResult<AdminEventListItem>.Create(items, page, pageSize, total));
    }
}

public sealed record GetAdminEventQuery(Guid EventId) : IQuery<AdminEventDetail>;

internal sealed class GetAdminEventQueryHandler : IQueryHandler<GetAdminEventQuery, AdminEventDetail>
{
    private const int MaxParticipants = 100;

    private readonly IApplicationDbContext _dbContext;

    public GetAdminEventQueryHandler(IApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<AdminEventDetail>> Handle(GetAdminEventQuery request, CancellationToken cancellationToken)
    {
        var @event = await _dbContext.Events.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == request.EventId, cancellationToken);
        if (@event is null)
        {
            return Result<AdminEventDetail>.Failure(AdminErrors.EventNotFound);
        }

        var row = new AdminEventRow(
            @event.Id, @event.Title, @event.Status, @event.EventDate, @event.CreatedAt,
            @event.OrganizerUserId, @event.OrganizationId, @event.SportId, @event.City, @event.MaxParticipants);
        var item = (await AdminEventMapper.BuildAsync(_dbContext, [row], cancellationToken))[0];

        var participantRows = await _dbContext.EventParticipants.AsNoTracking()
            .Where(participant => participant.EventId == @event.Id && participant.Status != ParticipantStatus.Cancelled)
            .OrderBy(participant => participant.CreatedAt)
            .Take(MaxParticipants)
            .Select(participant => new
            {
                participant.UserId,
                participant.GuestFirstName,
                participant.GuestLastName,
                participant.Status,
                participant.JoinedAt
            })
            .ToListAsync(cancellationToken);
        var refs = await AdminTargets.LoadUserRefsAsync(
            _dbContext, participantRows.Where(p => p.UserId != null).Select(p => p.UserId!.Value), cancellationToken);
        var participants = participantRows
            .Select(p =>
            {
                var guest = string.Join(' ', new[] { p.GuestFirstName, p.GuestLastName }
                    .Where(part => !string.IsNullOrWhiteSpace(part)));
                return new AdminEventParticipant(
                    p.UserId is { } userId ? refs.GetValueOrDefault(userId) : null,
                    guest.Length == 0 ? null : guest,
                    (short)p.Status,
                    p.JoinedAt);
            })
            .ToList();

        var reports = _dbContext.Reports.AsNoTracking()
            .Where(report => report.EntityType == ReportEntityType.Event && report.EntityId == @event.Id);
        var totalReports = await reports.CountAsync(cancellationToken);
        var openReports = await reports.CountAsync(
            report => report.Status == ReportStatus.Pending || report.Status == ReportStatus.UnderReview,
            cancellationToken);

        return Result<AdminEventDetail>.Success(new AdminEventDetail(
            item.Id, item.Title, item.Status, item.EventDate, item.CreatedAt, item.Organizer, item.Club,
            item.SportName, item.CityName, item.ParticipantCount, item.MaxParticipants, item.IsTestHosted,
            @event.Description,
            @event.District,
            @event.Address,
            @event.IsPaid,
            @event.FeeAmount,
            @event.IsOrganizationOnly,
            openReports,
            totalReports,
            participants));
    }
}
