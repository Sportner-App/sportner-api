using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Identity.UserProfiles.GetSportReportCard;

/// <summary>
/// Profildeki "spor karnesi": kullanıcının bir yıl içinde gerçekten oynadığı
/// (yoklamada "geldi" işaretlenen) etkinliklerden türetilen özet. Sayaçlar
/// (UserStatistics) yıl ayrımı yapmadığı için bu değerler katılım kayıtlarından
/// hesaplanıyor.
/// </summary>
public sealed record GetSportReportCardQuery(Guid UserId, int? Year = null)
    : IQuery<SportReportCardResponse>;

public sealed record SportReportCardResponse(
    int Year,
    int EventsPlayed,
    int EventsOrganized,
    int MinutesPlayed,
    int SportsCount,
    int TeammatesCount,
    decimal AttendanceRate,
    decimal AverageRating,
    int TotalReviews,
    IReadOnlyList<SportReportCardSportResponse> TopSports);

public sealed record SportReportCardSportResponse(
    string Name,
    string Slug,
    int EventsPlayed);

internal sealed class GetSportReportCardQueryHandler
    : IQueryHandler<GetSportReportCardQuery, SportReportCardResponse>
{
    private const int TopSportsLimit = 3;

    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public GetSportReportCardQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<SportReportCardResponse>> Handle(
        GetSportReportCardQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await _dbContext.UserProfiles.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.UserId == request.UserId, cancellationToken);

        if (profile is null)
        {
            return Result<SportReportCardResponse>.Failure(ProfileErrors.NotFound);
        }

        if (await ProfileQueries.CheckViewAccessAsync(
                _dbContext, profile, _currentUser.UserId, cancellationToken)
            is { } accessError)
        {
            return Result<SportReportCardResponse>.Failure(accessError);
        }

        var year = request.Year ?? _timeProvider.GetUtcNow().Year;
        var yearStart = new DateTimeOffset(year, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var yearEnd = yearStart.AddYears(1);

        var played = await (
                from participant in _dbContext.EventParticipants.AsNoTracking()
                join @event in _dbContext.Events.AsNoTracking()
                    on participant.EventId equals @event.Id
                join sport in _dbContext.Sports.AsNoTracking()
                    on @event.SportId equals sport.Id
                where participant.UserId == request.UserId
                    && participant.Status == ParticipantStatus.Attended
                    && @event.Status != EventStatus.Cancelled
                    && @event.EventDate >= yearStart
                    && @event.EventDate < yearEnd
                select new
                {
                    @event.Id,
                    @event.DurationMinutes,
                    sport.Name,
                    sport.Slug
                })
            .ToListAsync(cancellationToken);

        var eventIds = played.Select(item => item.Id).ToList();

        var teammatesCount = eventIds.Count == 0
            ? 0
            : await _dbContext.EventParticipants.AsNoTracking()
                .Where(participant =>
                    eventIds.Contains(participant.EventId)
                    && participant.UserId != null
                    && participant.UserId != request.UserId
                    && participant.Status == ParticipantStatus.Attended)
                .Select(participant => participant.UserId)
                .Distinct()
                .CountAsync(cancellationToken);

        var eventsOrganized = await _dbContext.Events.AsNoTracking()
            .CountAsync(
                @event => @event.OrganizerUserId == request.UserId
                    && @event.Status == EventStatus.Completed
                    && @event.EventDate >= yearStart
                    && @event.EventDate < yearEnd,
                cancellationToken);

        var statistics = await ProfileQueries.GetStatisticsAsync(
            _dbContext, request.UserId, cancellationToken);

        var topSports = played
            .GroupBy(item => new { item.Name, item.Slug })
            .Select(group => new SportReportCardSportResponse(
                group.Key.Name,
                group.Key.Slug,
                group.Count()))
            .OrderByDescending(item => item.EventsPlayed)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Result<SportReportCardResponse>.Success(new SportReportCardResponse(
            year,
            played.Count,
            eventsOrganized,
            played.Sum(item =>
                item.DurationMinutes ?? Sportner.Domain.Events.Event.DefaultDurationMinutes),
            topSports.Count,
            teammatesCount,
            statistics?.AttendanceRate ?? 0,
            statistics?.AverageRating ?? 0,
            statistics?.TotalReviews ?? 0,
            topSports.Take(TopSportsLimit).ToList()));
    }
}
