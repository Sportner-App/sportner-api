using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Identity.UserProfiles.Streaks;
using Sportner.Application.Features.Leaderboards;

namespace Sportner.Application.Features.Recaps;

/// <summary>
/// Yıl sonu özeti (Wrapped). <c>Year</c> verilmezse: 15 Aralık'tan itibaren bu yıl,
/// öncesinde geçen yıl. Gelecek yıl istenemez.
/// </summary>
public sealed record GetYearlyRecapQuery(int? Year) : IQuery<YearlyRecapResponse>;

public sealed record YearlyRecapResponse(
    int Year,
    bool IsReady,
    int EventsPlayed,
    int EventsOrganized,
    int MinutesPlayed,
    int SportsCount,
    IReadOnlyList<MonthlyRecapSportResponse> TopSports,
    int TeammatesCount,
    IReadOnlyList<MonthlyRecapPartnerResponse> TopPartners,
    string? TopDistrict,
    int DistrictsCount,
    int? BusiestWeekday,
    /// <summary>1–12; en çok etkinliğe katılınan ay.</summary>
    int? BusiestMonth,
    /// <summary>Ocak'tan Aralık'a 12 ayın etkinlik sayıları.</summary>
    IReadOnlyList<int> MonthlyCounts,
    int LongestStreakWeeks,
    IReadOnlyList<MonthlyRecapMedalResponse> Medals,
    IReadOnlyList<YearlyRecapCrownResponse> Crowns,
    /// <summary>
    /// Kişilik etiketi: explorer, captain, teamPlayer, unstoppable,
    /// weekendWarrior, risingStar.
    /// </summary>
    string Persona);

public sealed record YearlyRecapCrownResponse(int Month, string District);

internal sealed class GetYearlyRecapQueryHandler
    : IQueryHandler<GetYearlyRecapQuery, YearlyRecapResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public GetYearlyRecapQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<YearlyRecapResponse>> Handle(
        GetYearlyRecapQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<YearlyRecapResponse>.Failure(RecapErrors.NotAuthenticated);
        }

        var local = _timeProvider.GetUtcNow().ToOffset(StreakQueries.IstanbulOffset);
        var year = request.Year ?? RecapCalculator.DefaultYear(local);
        if (year > local.Year || year < 2000)
        {
            return Result<YearlyRecapResponse>.Failure(RecapErrors.InvalidYear);
        }

        return Result<YearlyRecapResponse>.Success(
            await RecapCalculator.BuildYearAsync(_dbContext, userId, year, cancellationToken));
    }
}
