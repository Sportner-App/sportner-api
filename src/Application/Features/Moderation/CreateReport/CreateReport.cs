using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Moderation;

namespace Sportner.Application.Features.Moderation.CreateReport;

public sealed record CreateReportCommand(
    short EntityType,
    Guid EntityId,
    Guid ReportReasonId,
    string? Description) : ICommand<ReportResponse>;

public sealed class CreateReportCommandValidator : AbstractValidator<CreateReportCommand>
{
    public CreateReportCommandValidator()
    {
        RuleFor(command => command.EntityId).NotEmpty();
        RuleFor(command => command.ReportReasonId).NotEmpty();
        RuleFor(command => command.Description).MaximumLength(2000);
        RuleFor(command => command.EntityType)
            .Must(type => Enum.IsDefined((ReportEntityType)type))
            .WithMessage("The report entity type is invalid.");
    }
}

internal sealed class CreateReportCommandHandler : ICommandHandler<CreateReportCommand, ReportResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public CreateReportCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<ReportResponse>> Handle(
        CreateReportCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } reporterUserId)
        {
            return Result<ReportResponse>.Failure(ReportErrors.NotAuthenticated);
        }

        if (!Enum.IsDefined((ReportEntityType)request.EntityType))
        {
            return Result<ReportResponse>.Failure(ReportErrors.InvalidEntityType);
        }

        var entityType = (ReportEntityType)request.EntityType;

        var reason = await _dbContext.ReportReasons
            .FirstOrDefaultAsync(candidate => candidate.Id == request.ReportReasonId, cancellationToken);

        if (reason is null || !reason.IsSelectable())
        {
            return Result<ReportResponse>.Failure(ReportErrors.ReasonNotFound);
        }

        if (!await ReportQueries.TargetExistsAsync(
                _dbContext,
                entityType,
                request.EntityId,
                cancellationToken))
        {
            return Result<ReportResponse>.Failure(ReportErrors.TargetNotFound);
        }

        if (await ReportQueries.IsOwnTargetAsync(
                _dbContext,
                reporterUserId,
                entityType,
                request.EntityId,
                cancellationToken))
        {
            return Result<ReportResponse>.Failure(ReportErrors.CannotReportSelf);
        }

        var duplicate = await _dbContext.Reports.AsNoTracking()
            .AnyAsync(
                report =>
                    report.ReporterUserId == reporterUserId
                    && report.EntityType == entityType
                    && report.EntityId == request.EntityId,
                cancellationToken);

        if (duplicate)
        {
            return Result<ReportResponse>.Failure(ReportErrors.AlreadyExists);
        }

        var utcNow = _timeProvider.GetUtcNow();
        var report = Report.Create(
            reporterUserId,
            entityType,
            request.EntityId,
            reason.Id,
            request.Description,
            utcNow);

        _dbContext.Reports.Add(report);

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
