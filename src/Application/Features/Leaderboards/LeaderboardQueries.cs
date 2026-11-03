using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Identity.UserProfiles;
using Sportner.Application.Features.Social;

namespace Sportner.Application.Features.Leaderboards;

public sealed record GetDistrictLeaderboardQuery(
    string City,
    string District,
    Guid? SportId,
    LeaderboardPeriod Period) : IQuery<DistrictLeaderboardResponse>;

internal sealed class GetDistrictLeaderboardQueryHandler
    : IQueryHandler<GetDistrictLeaderboardQuery, DistrictLeaderboardResponse>
{
    private const int TopCount = 20;

    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public GetDistrictLeaderboardQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<DistrictLeaderboardResponse>> Handle(
        GetDistrictLeaderboardQuery request,
        CancellationToken cancellationToken)
    {
        var month = LeaderboardCalculator.MonthOf(_timeProvider.GetUtcNow());
        var windowStart = request.Period == LeaderboardPeriod.Month
            ? LeaderboardCalculator.StartOf(month)
            : (DateTimeOffset?)null;

        var rows = await LeaderboardCalculator.LoadRowsAsync(
            _dbContext,
            request.City.Trim(),
            request.District.Trim(),
            request.SportId,
            windowStart,
            windowEnd: null,
            cancellationToken);

        // Engellenenler isteyene görünmez; sıra numarası yine herkes için aynı kalır.
        var hidden = _currentUser.UserId is { } viewerId
            ? (await BlockQueries.BlockedUserIds(_dbContext, viewerId).ToListAsync(cancellationToken))
                .ToHashSet()
            : [];

        var standings = LeaderboardCalculator.Rank(rows);
        var visible = standings.Where(standing => !hidden.Contains(standing.UserId)).ToList();
        var me = _currentUser.UserId is { } userId
            ? standings.FirstOrDefault(standing => standing.UserId == userId)
            : null;

        var top = standings.Count >= LeaderboardCalculator.MinPlayers
            ? visible.Take(TopCount).ToList()
            : [];
        var profileIds = top.Select(standing => standing.UserId)
            .Concat(me is null ? [] : [me.UserId])
            .Distinct()
            .ToList();
        var profiles = await _dbContext.UserProfiles.AsNoTracking()
            .Where(profile => profileIds.Contains(profile.UserId))
            .ToDictionaryAsync(profile => profile.UserId, cancellationToken);

        LeaderboardEntryResponse? ToEntry(LeaderboardCalculator.Standing standing) =>
            profiles.TryGetValue(standing.UserId, out var profile)
                ? new LeaderboardEntryResponse(
                    standing.Rank,
                    standing.UserId,
                    profile.Username,
                    string.Join(' ', new[] { profile.FirstName, profile.LastName }
                        .Where(part => !string.IsNullOrWhiteSpace(part))),
                    profile.ProfileImageUrl,
                    standing.EventCount)
                : null;

        return Result<DistrictLeaderboardResponse>.Success(new DistrictLeaderboardResponse(
            request.City.Trim(),
            request.District.Trim(),
            request.Period,
            standings.Count,
            LeaderboardCalculator.MinPlayers,
            top.Select(ToEntry).OfType<LeaderboardEntryResponse>().ToList(),
            me is null ? null : ToEntry(me)));
    }
}

/// <summary>Bir şehrin semtleri, bu ayki oyuncu sayısına göre (semt seçici için).</summary>
public sealed record ListDistrictsQuery(string? City) : IQuery<IReadOnlyList<DistrictSummaryResponse>>;

internal sealed class ListDistrictsQueryHandler
    : IQueryHandler<ListDistrictsQuery, IReadOnlyList<DistrictSummaryResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public ListDistrictsQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<IReadOnlyList<DistrictSummaryResponse>>> Handle(
        ListDistrictsQuery request,
        CancellationToken cancellationToken)
    {
        var city = request.City?.Trim();
        if (string.IsNullOrEmpty(city) && _currentUser.UserId is { } userId)
        {
            city = await _dbContext.UserProfiles.AsNoTracking()
                .Where(profile => profile.UserId == userId)
                .Select(profile => profile.City)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var monthStart = LeaderboardCalculator.StartOf(
            LeaderboardCalculator.MonthOf(_timeProvider.GetUtcNow()));
        var rows = await LeaderboardCalculator.LoadRowsAsync(
            _dbContext, string.IsNullOrEmpty(city) ? null : city, null, null, null, null, cancellationToken);

        var districts = rows
            .GroupBy(row => (row.City, row.District))
            .Select(group => new DistrictSummaryResponse(
                group.Key.City,
                group.Key.District,
                group.Where(row => row.EventDate >= monthStart).Select(row => row.UserId).Distinct().Count(),
                group.Select(row => row.EventId).Distinct().Count()))
            .OrderByDescending(item => item.PlayersThisMonth)
            .ThenByDescending(item => item.EventsAllTime)
            .ThenBy(item => item.District, StringComparer.CurrentCulture)
            .ToList();

        return Result<IReadOnlyList<DistrictSummaryResponse>>.Success(districts);
    }
}

/// <summary>
/// Ana sayfa kartı: kullanıcının en çok oynadığı semtteki bu ayki sırası.
/// Hiç oynamadıysa şehrinin en hareketli semti, sırasız.
/// </summary>
public sealed record GetMyDistrictStandingQuery : IQuery<MyDistrictStandingResponse?>;

internal sealed class GetMyDistrictStandingQueryHandler
    : IQueryHandler<GetMyDistrictStandingQuery, MyDistrictStandingResponse?>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public GetMyDistrictStandingQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<MyDistrictStandingResponse?>> Handle(
        GetMyDistrictStandingQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<MyDistrictStandingResponse?>.Failure(ProfileErrors.NotAuthenticated);
        }

        var home = await (
                from participant in _dbContext.EventParticipants.AsNoTracking()
                join @event in _dbContext.Events.AsNoTracking() on participant.EventId equals @event.Id
                where participant.UserId == userId
                    && participant.Status == Domain.Common.Enums.ParticipantStatus.Attended
                    && @event.District != null
                    && @event.City != null
                group @event by new { @event.City, @event.District } into districtGroup
                orderby districtGroup.Count() descending
                select new { districtGroup.Key.City, districtGroup.Key.District })
            .FirstOrDefaultAsync(cancellationToken);

        var city = home?.City;
        var district = home?.District;

        if (district is null)
        {
            var profileCity = await _dbContext.UserProfiles.AsNoTracking()
                .Where(profile => profile.UserId == userId)
                .Select(profile => profile.City)
                .FirstOrDefaultAsync(cancellationToken);

            var busiest = await _dbContext.Events.AsNoTracking()
                .Where(@event => @event.District != null && @event.City != null
                    && (profileCity == null || @event.City == profileCity))
                .GroupBy(@event => new { @event.City, @event.District })
                .OrderByDescending(group => group.Count())
                .Select(group => group.Key)
                .FirstOrDefaultAsync(cancellationToken);

            if (busiest is null)
            {
                return Result<MyDistrictStandingResponse?>.Success(null);
            }

            city = busiest.City;
            district = busiest.District;
        }

        var rows = await LeaderboardCalculator.LoadRowsAsync(
            _dbContext,
            city,
            district,
            null,
            LeaderboardCalculator.StartOf(LeaderboardCalculator.MonthOf(_timeProvider.GetUtcNow())),
            null,
            cancellationToken);
        var standings = LeaderboardCalculator.Rank(rows);
        var me = standings.FirstOrDefault(standing => standing.UserId == userId);

        return Result<MyDistrictStandingResponse?>.Success(new MyDistrictStandingResponse(
            city!, district!, me?.Rank, me?.EventCount ?? 0, standings.Count));
    }
}

public sealed record ListDistrictCrownsQuery(Guid UserId) : IQuery<IReadOnlyList<DistrictCrownResponse>>;

internal sealed class ListDistrictCrownsQueryHandler
    : IQueryHandler<ListDistrictCrownsQuery, IReadOnlyList<DistrictCrownResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListDistrictCrownsQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<DistrictCrownResponse>>> Handle(
        ListDistrictCrownsQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await _dbContext.UserProfiles.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.UserId == request.UserId, cancellationToken);

        if (profile is null)
        {
            return Result<IReadOnlyList<DistrictCrownResponse>>.Failure(ProfileErrors.NotFound);
        }

        if (await ProfileQueries.CheckViewAccessAsync(
                _dbContext, profile, _currentUser.UserId, cancellationToken)
            is { } accessError)
        {
            return Result<IReadOnlyList<DistrictCrownResponse>>.Failure(accessError);
        }

        var crowns = await _dbContext.DistrictLeaders.AsNoTracking()
            .Where(leader => leader.UserId == request.UserId && leader.CrownedAt != null)
            .OrderByDescending(leader => leader.Month)
            .Select(leader => new DistrictCrownResponse(
                leader.City, leader.District, leader.Month, leader.EventCount))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<DistrictCrownResponse>>.Success(crowns);
    }
}
