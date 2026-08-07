using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Application.Features.Moderation.UpdateReportDescription;

public sealed record UpdateReportDescriptionCommand(Guid ReportId, string? Description)
    : ICommand<ReportResponse>;

public sealed class UpdateReportDescriptionCommandValidator
    : AbstractValidator<UpdateReportDescriptionCommand>
{
    public UpdateReportDescriptionCommandValidator()
    {
        RuleFor(command => command.ReportId).NotEmpty();
        RuleFor(command => command.Description).MaximumLength(2000);
    }
}

internal sealed class UpdateReportDescriptionCommandHandler
    : ICommandHandler<UpdateReportDescriptionCommand, ReportResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public UpdateReportDescriptionCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<ReportResponse>> Handle(
        UpdateReportDescriptionCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<ReportResponse>.Failure(ReportErrors.NotAuthenticated);
        }

        var report = await _dbContext.Reports
            .FirstOrDefaultAsync(candidate => candidate.Id == request.ReportId, cancellationToken);

        if (report is null)
        {
            return Result<ReportResponse>.Failure(ReportErrors.NotFound);
        }

        if (!report.WasReportedBy(userId))
        {
            return Result<ReportResponse>.Failure(ReportErrors.NotOwner);
        }

        try
        {
            report.UpdateDescription(request.Description, _timeProvider.GetUtcNow());
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
