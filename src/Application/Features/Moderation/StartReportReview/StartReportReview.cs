using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Application.Features.Moderation.StartReportReview;

public sealed record StartReportReviewCommand(Guid ReportId) : ICommand<ReportResponse>;

internal sealed class StartReportReviewCommandHandler
    : ICommandHandler<StartReportReviewCommand, ReportResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public StartReportReviewCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<ReportResponse>> Handle(
        StartReportReviewCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } moderatorUserId)
        {
            return Result<ReportResponse>.Failure(ReportErrors.NotAuthenticated);
        }

        var report = await _dbContext.Reports
            .FirstOrDefaultAsync(candidate => candidate.Id == request.ReportId, cancellationToken);

        if (report is null)
        {
            return Result<ReportResponse>.Failure(ReportErrors.NotFound);
        }

        try
        {
            report.StartReview(moderatorUserId, _timeProvider.GetUtcNow());
        }
        catch (DomainException)
        {
            return Result<ReportResponse>.Failure(ReportErrors.InvalidOperation);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = await ReportQueries.Project(_dbContext)
            .FirstAsync(candidate => candidate.Id == report.Id, cancellationToken);

        return Result<ReportResponse>.Success(response);
    }
}
