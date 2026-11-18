using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Models;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Organizations;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Admin.Clubs;

public sealed record AdminClubListItem(
    Guid Id,
    string Name,
    string? ImageUrl,
    bool IsPublic,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DeletedAt,
    AdminUserRef? Founder,
    string? CityName,
    int MemberCount,
    int EventCount,
    bool IsTestFounded);

public sealed record AdminClubMember(AdminUserRef? User, short Role, short Status, DateTimeOffset? JoinedAt);

public sealed record AdminClubEvent(Guid Id, string Title, short Status, DateTimeOffset EventDate);

public sealed record AdminClubDetail(
    Guid Id,
    string Name,
    string? ImageUrl,
    bool IsPublic,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DeletedAt,
    AdminUserRef? Founder,
    string? CityName,
    int MemberCount,
    int EventCount,
    bool IsTestFounded,
    string? Description,
    short EventCreationPolicy,
    IReadOnlyList<AdminClubMember> Members,
    IReadOnlyList<AdminClubEvent> RecentEvents,
    int OpenReports);

internal sealed record AdminClubRow(
    Guid Id,
    string Name,
    string? ImageUrl,
    bool IsPublic,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DeletedAt,
    Guid FounderUserId,
    Guid? CityId);

internal static class AdminClubMapper
{
    internal static async Task<List<AdminClubListItem>> BuildAsync(
        IApplicationDbContext dbContext,
        IReadOnlyList<AdminClubRow> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return [];
        }

        var clubIds = rows.Select(row => row.Id).ToList();
        var founderIds = rows.Select(row => row.FounderUserId).Distinct().ToList();
        var cityIds = rows.Where(row => row.CityId != null).Select(row => row.CityId!.Value).Distinct().ToList();

        var founders = await AdminTargets.LoadUserRefsAsync(dbContext, founderIds, cancellationToken);
        var testIds = (await dbContext.Users.AsNoTracking()
                .Where(user => founderIds.Contains(user.Id) && user.IsTestAccount)
                .Select(user => user.Id)
                .ToListAsync(cancellationToken))
            .ToHashSet();
        var cities = cityIds.Count == 0
            ? []
            : await dbContext.Cities.AsNoTracking()
                .Where(city => cityIds.Contains(city.Id))
                .Select(city => new { city.Id, city.Name })
                .ToDictionaryAsync(city => city.Id, city => city.Name, cancellationToken);
        var members = await dbContext.OrganizationMembers.AsNoTracking()
            .Where(member => clubIds.Contains(member.OrganizationId)
                && member.Status == OrganizationMemberStatus.Approved)
            .GroupBy(member => member.OrganizationId)
            .Select(group => new { ClubId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.ClubId, group => group.Count, cancellationToken);
        var events = await dbContext.Events.AsNoTracking()
            .Where(@event => @event.OrganizationId != null && clubIds.Contains(@event.OrganizationId.Value))
            .GroupBy(@event => @event.OrganizationId!.Value)
            .Select(group => new { ClubId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.ClubId, group => group.Count, cancellationToken);

        return rows.Select(row => new AdminClubListItem(
                row.Id,
                row.Name,
                row.ImageUrl,
                row.IsPublic,
                row.CreatedAt,
                row.DeletedAt,
                founders.GetValueOrDefault(row.FounderUserId),
                row.CityId is { } cityId ? cities.GetValueOrDefault(cityId) : null,
                members.GetValueOrDefault(row.Id),
                events.GetValueOrDefault(row.Id),
                testIds.Contains(row.FounderUserId)))
            .ToList();
    }
}

public sealed record ListAdminClubsQuery(
    string? Search, string? Visibility, bool? IncludeDeleted, int? Page, int? PageSize)
    : IQuery<PagedResult<AdminClubListItem>>;

internal sealed class ListAdminClubsQueryHandler
    : IQueryHandler<ListAdminClubsQuery, PagedResult<AdminClubListItem>>
{
    private readonly IApplicationDbContext _dbContext;

    public ListAdminClubsQueryHandler(IApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<PagedResult<AdminClubListItem>>> Handle(
        ListAdminClubsQuery request,
        CancellationToken cancellationToken)
    {
        var (page, pageSize) = AdminPaging.Normalize(request.Page, request.PageSize);

        // Silinmiş kulüpler global filtreyle gizlenir; yönetici isterse görür.
        var query = _dbContext.Organizations.AsNoTracking().IgnoreQueryFilters();
        if (request.IncludeDeleted is not true)
        {
            query = query.Where(club => club.DeletedAt == null);
        }

        switch (request.Visibility?.Trim().ToLowerInvariant())
        {
            case "public":
                query = query.Where(club => club.IsPublic);
                break;
            case "private":
                query = query.Where(club => !club.IsPublic);
                break;
        }

        var search = request.Search?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(search))
        {
            query = Guid.TryParse(search, out var searchId)
                ? query.Where(club => club.Id == searchId)
                : query.Where(club => club.Name.ToLower().Contains(search));
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(club => club.CreatedAt)
            .ThenBy(club => club.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(club => new AdminClubRow(
                club.Id, club.Name, club.ImageUrl, club.IsPublic, club.CreatedAt, club.DeletedAt,
                club.FounderUserId, club.CityId))
            .ToListAsync(cancellationToken);

        var items = await AdminClubMapper.BuildAsync(_dbContext, rows, cancellationToken);
        return Result<PagedResult<AdminClubListItem>>.Success(
            PagedResult<AdminClubListItem>.Create(items, page, pageSize, total));
    }
}

public sealed record GetAdminClubQuery(Guid ClubId) : IQuery<AdminClubDetail>;

internal sealed class GetAdminClubQueryHandler : IQueryHandler<GetAdminClubQuery, AdminClubDetail>
{
    private const int MaxMembers = 100;
    private const int MaxRecentEvents = 10;

    private readonly IApplicationDbContext _dbContext;

    public GetAdminClubQueryHandler(IApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<AdminClubDetail>> Handle(GetAdminClubQuery request, CancellationToken cancellationToken)
    {
        var club = await _dbContext.Organizations.AsNoTracking().IgnoreQueryFilters()
            .FirstOrDefaultAsync(candidate => candidate.Id == request.ClubId, cancellationToken);
        if (club is null)
        {
            return Result<AdminClubDetail>.Failure(AdminErrors.ClubNotFound);
        }

        var row = new AdminClubRow(
            club.Id, club.Name, club.ImageUrl, club.IsPublic, club.CreatedAt, club.DeletedAt,
            club.FounderUserId, club.CityId);
        var item = (await AdminClubMapper.BuildAsync(_dbContext, [row], cancellationToken))[0];

        var memberRows = await _dbContext.OrganizationMembers.AsNoTracking()
            .Where(member => member.OrganizationId == club.Id
                && (member.Status == OrganizationMemberStatus.Approved
                    || member.Status == OrganizationMemberStatus.Pending))
            .OrderBy(member => member.CreatedAt)
            .Take(MaxMembers)
            .Select(member => new { member.UserId, member.Role, member.Status, member.RespondedAt, member.CreatedAt })
            .ToListAsync(cancellationToken);
        var refs = await AdminTargets.LoadUserRefsAsync(
            _dbContext, memberRows.Select(member => member.UserId), cancellationToken);
        var members = memberRows
            .Select(member => new AdminClubMember(
                refs.GetValueOrDefault(member.UserId),
                (short)member.Role,
                (short)member.Status,
                member.Status == OrganizationMemberStatus.Approved ? member.RespondedAt ?? member.CreatedAt : null))
            .ToList();

        var recent = await _dbContext.Events.AsNoTracking()
            .Where(@event => @event.OrganizationId == club.Id)
            .OrderByDescending(@event => @event.EventDate)
            .Take(MaxRecentEvents)
            .Select(@event => new { @event.Id, @event.Title, @event.Status, @event.EventDate })
            .ToListAsync(cancellationToken);

        return Result<AdminClubDetail>.Success(new AdminClubDetail(
            item.Id, item.Name, item.ImageUrl, item.IsPublic, item.CreatedAt, item.DeletedAt, item.Founder,
            item.CityName, item.MemberCount, item.EventCount, item.IsTestFounded,
            club.Description,
            (short)club.EventCreationPolicy,
            members,
            recent.Select(@event => new AdminClubEvent(@event.Id, @event.Title, (short)@event.Status, @event.EventDate)).ToList(),
            // Şikâyet varlık türleri arasında kulüp yok.
            0));
    }
}

public sealed record SetClubVisibilityCommand(Guid ClubId, bool IsPublic) : ICommand;

internal sealed class SetClubVisibilityCommandHandler : ICommandHandler<SetClubVisibilityCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public SetClubVisibilityCommandHandler(
        IApplicationDbContext dbContext, ICurrentUser currentUser, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(SetClubVisibilityCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } adminId)
        {
            return Result.Failure(AdminErrors.NotAuthenticated);
        }

        var club = await _dbContext.Organizations.IgnoreQueryFilters()
            .FirstOrDefaultAsync(candidate => candidate.Id == request.ClubId, cancellationToken);
        if (club is null)
        {
            return Result.Failure(AdminErrors.ClubNotFound);
        }

        if (club.IsDeleted)
        {
            return Result.Failure(AdminErrors.ClubDeleted);
        }

        if (club.IsPublic == request.IsPublic)
        {
            return Result.Success();
        }

        var utcNow = _timeProvider.GetUtcNow();
        club.UpdateDetails(club.Name, club.Description, club.CityId, request.IsPublic, utcNow);
        _dbContext.AdminAuditEntries.Add(AdminActions.Entry(
            adminId, AdminActions.ClubVisibility, AdminActions.ClubTarget, club.Id,
            request.IsPublic ? "public" : "private", utcNow));
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>Kulübü kapatır; başlamamış etkinlikleri nötr bildirimle iptal eder.</summary>
public sealed record CloseClubCommand(Guid ClubId) : ICommand;

internal sealed class CloseClubCommandHandler : ICommandHandler<CloseClubCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly INotificationPublisher _notificationPublisher;

    public CloseClubCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        INotificationPublisher notificationPublisher)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _notificationPublisher = notificationPublisher;
    }

    public async Task<Result> Handle(CloseClubCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } adminId)
        {
            return Result.Failure(AdminErrors.NotAuthenticated);
        }

        var club = await _dbContext.Organizations.IgnoreQueryFilters()
            .FirstOrDefaultAsync(candidate => candidate.Id == request.ClubId, cancellationToken);
        if (club is null)
        {
            return Result.Failure(AdminErrors.ClubNotFound);
        }

        if (club.IsDeleted)
        {
            return Result.Failure(AdminErrors.ClubDeleted);
        }

        var utcNow = _timeProvider.GetUtcNow();
        var cancelled = await OrganizationClosure.CloseAsync(
            _dbContext, _notificationPublisher, club, adminId, utcNow, cancellationToken, cancelledByAdmin: true);

        _dbContext.AdminAuditEntries.Add(AdminActions.Entry(
            adminId, AdminActions.ClubClose, AdminActions.ClubTarget, club.Id,
            cancelled.ToString(System.Globalization.CultureInfo.InvariantCulture), utcNow));
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
