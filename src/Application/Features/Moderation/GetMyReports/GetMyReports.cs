using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Moderation.GetMyReports;

public sealed record GetMyReportsQuery(
    int Page = 1,
    int PageSize = 20) : IQuery<IReadOnlyList<ReportResponse>>;

internal sealed class GetMyReportsQueryHandler
    : IQueryHandler<GetMyReportsQuery, IReadOnlyList<ReportResponse>>
{
    private const int MaxPageSize = 100;

    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetMyReportsQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<ReportResponse>>> Handle(
        GetMyReportsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<IReadOnlyList<ReportResponse>>.Failure(ReportErrors.NotAuthenticated);
        }

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > MaxPageSize ? 20 : request.PageSize;

        var reports = await ReportQueries.Project(_dbContext)
            .Where(report => report.ReporterUserId == userId)
            .OrderByDescending(report => report.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<ReportResponse>>.Success(reports);
    }
}
