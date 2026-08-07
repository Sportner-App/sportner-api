using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Moderation.ListReports;

public sealed record ListReportsQuery(
    short? Status = null,
    int Page = 1,
    int PageSize = 20) : IQuery<IReadOnlyList<ReportResponse>>;

internal sealed class ListReportsQueryHandler
    : IQueryHandler<ListReportsQuery, IReadOnlyList<ReportResponse>>
{
    private const int MaxPageSize = 100;

    private readonly IApplicationDbContext _dbContext;

    public ListReportsQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<ReportResponse>>> Handle(
        ListReportsQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > MaxPageSize ? 20 : request.PageSize;

        var query = ReportQueries.Project(_dbContext);

        if (request.Status is { } statusValue)
        {
            if (!Enum.IsDefined((ReportStatus)statusValue))
            {
                return Result<IReadOnlyList<ReportResponse>>.Failure(ReportErrors.InvalidOperation);
            }

            query = query.Where(report => report.Status == statusValue);
        }

        var reports = await query
            .OrderByDescending(report => report.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<ReportResponse>>.Success(reports);
    }
}
