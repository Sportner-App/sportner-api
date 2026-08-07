using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Application.Features.Moderation.ResolveReport;

public sealed record ResolveReportCommand(Guid ReportId, string ResolutionNote)
    : ICommand<ReportResponse>;

public sealed class ResolveReportCommandValidator : AbstractValidator<ResolveReportCommand>
{
    public ResolveReportCommandValidator()
    {
        RuleFor(command => command.ReportId).NotEmpty();
        RuleFor(command => command.ResolutionNote).NotEmpty().MaximumLength(2000);
    }
}

internal sealed class ResolveReportCommandHandler
    : ICommandHandler<ResolveReportCommand, ReportResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public ResolveReportCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<ReportResponse>> Handle(
        ResolveReportCommand request,
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

        var utcNow = _timeProvider.GetUtcNow();

        try
        {
            report.Resolve(moderatorUserId, request.ResolutionNote, utcNow);
        }
        catch (DomainException)
        {
            return Result<ReportResponse>.Failure(ReportErrors.InvalidOperation);
        }

        await ReportQueries.ApplyReviewSideEffectsAsync(
            _dbContext,
            report,
            markReported: true,
            utcNow,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = await ReportQueries.Project(_dbContext)
            .FirstAsync(candidate => candidate.Id == report.Id, cancellationToken);

        return Result<ReportResponse>.Success(response);
    }
}
