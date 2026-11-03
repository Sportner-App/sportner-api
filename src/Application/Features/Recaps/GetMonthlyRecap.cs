using System.Globalization;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Leaderboards;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Recaps;

/// <summary>
/// Kullanıcının kendi aylık özeti. <c>Month</c> verilmezse geçen ay
/// (İstanbul takvimi). İçinde bulunulan aydan sonrası istenemez.
/// </summary>
public sealed record GetMonthlyRecapQuery(string? Month) : IQuery<MonthlyRecapResponse>;

public sealed record MonthlyRecapResponse(
    string Month,
    bool IsReady,
    int EventsPlayed,
    int PreviousMonthEventsPlayed,
    int MinutesPlayed,
    int SportsCount,
    IReadOnlyList<MonthlyRecapSportResponse> TopSports,
    int TeammatesCount,
    MonthlyRecapPartnerResponse? TopPartner,
    string? TopDistrict,
    int DistrictsCount,
    /// <summary>0 = Pazar … 6 = Cumartesi.</summary>
    int? BusiestWeekday,
    IReadOnlyList<MonthlyRecapMedalResponse> Medals,
    string? CrownDistrict);

public sealed record MonthlyRecapSportResponse(string Name, string Slug, int EventsPlayed);

public sealed record MonthlyRecapPartnerResponse(
    Guid UserId,
    string Username,
    string FullName,
    string? ProfileImageUrl,
    int EventsTogether);

public sealed record MonthlyRecapMedalResponse(string Title, string Icon);

internal static class RecapErrors
{
    internal static Error NotAuthenticated => Error.Unauthorized(
        "Recap.NotAuthenticated",
        ErrorMessagesResource.Profile_NotAuthenticated);

    internal static Error InvalidMonth => Error.Validation(
        "Recap.InvalidMonth",
        ErrorMessagesResource.Recap_InvalidMonth);

    internal static Error InvalidYear => Error.Validation(
        "Recap.InvalidYear",
        ErrorMessagesResource.Recap_InvalidYear);
}

internal sealed class GetMonthlyRecapQueryHandler
    : IQueryHandler<GetMonthlyRecapQuery, MonthlyRecapResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public GetMonthlyRecapQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<MonthlyRecapResponse>> Handle(
        GetMonthlyRecapQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<MonthlyRecapResponse>.Failure(RecapErrors.NotAuthenticated);
        }

        var current = LeaderboardCalculator.MonthOf(_timeProvider.GetUtcNow());
        DateOnly month;
        if (string.IsNullOrWhiteSpace(request.Month))
        {
            month = current.AddMonths(-1);
        }
        else if (!DateOnly.TryParseExact(
                     request.Month.Trim() + "-01",
                     "yyyy-MM-dd",
                     CultureInfo.InvariantCulture,
                     DateTimeStyles.None,
                     out month)
                 || month > current)
        {
            return Result<MonthlyRecapResponse>.Failure(RecapErrors.InvalidMonth);
        }

        return Result<MonthlyRecapResponse>.Success(
            await RecapCalculator.BuildAsync(_dbContext, userId, month, cancellationToken));
    }
}
