using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Moderation.ListActiveReportReasons;

public sealed record ListActiveReportReasonsQuery : IQuery<IReadOnlyList<ReportReasonResponse>>;

internal sealed class ListActiveReportReasonsQueryHandler
    : IQueryHandler<ListActiveReportReasonsQuery, IReadOnlyList<ReportReasonResponse>>
{
    private readonly IApplicationDbContext _dbContext;

    public ListActiveReportReasonsQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<ReportReasonResponse>>> Handle(
        ListActiveReportReasonsQuery request,
        CancellationToken cancellationToken)
    {
        var reasons = await _dbContext.ReportReasons.AsNoTracking()
            .Where(reason => reason.IsActive)
            .OrderBy(reason => reason.DisplayOrder)
            .ThenBy(reason => reason.Code)
            .Select(reason => new ReportReasonResponse(
                reason.Id,
                reason.Code,
                reason.Name,
                reason.Description,
                reason.DisplayOrder))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<ReportReasonResponse>>.Success(reasons);
    }
}
