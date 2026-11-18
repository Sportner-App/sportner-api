using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Gamification;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Abstractions.Storage;
using Sportner.Application.Common.Models;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Moderation;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Admin.Reports;

public sealed record AdminReportItem(
    Guid Id,
    short EntityType,
    Guid EntityId,
    string? ReasonCode,
    string? ReasonName,
    string? Description,
    short Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReviewedAt,
    string? ResolutionNote,
    AdminUserRef? Reporter,
    AdminReportTarget Target,
    int OpenReportsOnTarget);

public sealed record ListAdminReportsQuery(short? Status, short? EntityType, int? Page, int? PageSize)
    : IQuery<PagedResult<AdminReportItem>>;

internal sealed class ListAdminReportsQueryHandler
    : IQueryHandler<ListAdminReportsQuery, PagedResult<AdminReportItem>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IFileStorage _fileStorage;

    public ListAdminReportsQueryHandler(IApplicationDbContext dbContext, IFileStorage fileStorage)
    {
        _dbContext = dbContext;
        _fileStorage = fileStorage;
    }

    public async Task<Result<PagedResult<AdminReportItem>>> Handle(
        ListAdminReportsQuery request,
        CancellationToken cancellationToken)
    {
        var (page, pageSize) = AdminPaging.Normalize(request.Page, request.PageSize);
        var query = _dbContext.Reports.AsNoTracking();

        if (request.Status is { } status)
        {
            var typed = (ReportStatus)status;
            query = query.Where(report => report.Status == typed);
        }

        if (request.EntityType is { } entityType)
        {
            var typed = (ReportEntityType)entityType;
            query = query.Where(report => report.EntityType == typed);
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await (
                from report in query
                join reason in _dbContext.ReportReasons.AsNoTracking()
                    on report.ReportReasonId equals reason.Id into reasons
                from reason in reasons.DefaultIfEmpty()
                orderby report.CreatedAt descending, report.Id
                select new
                {
                    report.Id,
                    report.EntityType,
                    report.EntityId,
                    ReasonCode = reason != null ? reason.Code : null,
                    ReasonName = reason != null ? reason.Name : null,
                    report.Description,
                    report.Status,
                    report.CreatedAt,
                    report.ReviewedAt,
                    report.ResolutionNote,
                    report.ReporterUserId
                })
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var keys = rows.Select(row => (row.EntityType, row.EntityId)).Distinct().ToList();
        var targets = await AdminTargets.ResolveAsync(_dbContext, _fileStorage, keys, cancellationToken);
        var reporters = await AdminTargets.LoadUserRefsAsync(
            _dbContext, rows.Select(row => row.ReporterUserId), cancellationToken);

        // Aynı hedef için açık (bekleyen + incelemede) şikâyet sayısı; sayfadaki hedefler için tek sorgu.
        var entityIds = keys.Select(key => key.EntityId).Distinct().ToList();
        var openRows = entityIds.Count == 0
            ? []
            : await _dbContext.Reports.AsNoTracking()
                .Where(report => entityIds.Contains(report.EntityId)
                    && (report.Status == ReportStatus.Pending || report.Status == ReportStatus.UnderReview))
                .GroupBy(report => new { report.EntityType, report.EntityId })
                .Select(group => new { group.Key.EntityType, group.Key.EntityId, Count = group.Count() })
                .ToListAsync(cancellationToken);
        var openCounts = openRows.ToDictionary(row => (row.EntityType, row.EntityId), row => row.Count);

        var items = rows
            .Select(row => new AdminReportItem(
                row.Id,
                (short)row.EntityType,
                row.EntityId,
                row.ReasonCode,
                row.ReasonName,
                row.Description,
                (short)row.Status,
                row.CreatedAt,
                row.ReviewedAt,
                row.ResolutionNote,
                reporters.GetValueOrDefault(row.ReporterUserId),
                targets[(row.EntityType, row.EntityId)],
                openCounts.GetValueOrDefault((row.EntityType, row.EntityId))))
            .ToList();

        return Result<PagedResult<AdminReportItem>>.Success(
            PagedResult<AdminReportItem>.Create(items, page, pageSize, total));
    }
}

public sealed record AdminResolveReportCommand(Guid ReportId, string Note) : ICommand;

public sealed class AdminResolveReportCommandValidator : AbstractValidator<AdminResolveReportCommand>
{
    public AdminResolveReportCommandValidator()
    {
        RuleFor(command => command.ReportId).NotEmpty();
        RuleFor(command => command.Note).NotEmpty().MaximumLength(2000)
            .WithMessage(_ => ErrorMessagesResource.Admin_InvalidNote);
    }
}

internal sealed class AdminResolveReportCommandHandler : ICommandHandler<AdminResolveReportCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly IBadgeAwarder _badgeAwarder;

    public AdminResolveReportCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        IBadgeAwarder badgeAwarder)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _badgeAwarder = badgeAwarder;
    }

    public async Task<Result> Handle(AdminResolveReportCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } adminId)
        {
            return Result.Failure(AdminErrors.NotAuthenticated);
        }

        var report = await _dbContext.Reports
            .FirstOrDefaultAsync(candidate => candidate.Id == request.ReportId, cancellationToken);
        if (report is null)
        {
            return Result.Failure(AdminErrors.ReportNotFound);
        }

        if (report.Status is ReportStatus.Resolved)
        {
            return Result.Success();
        }

        var utcNow = _timeProvider.GetUtcNow();
        var note = request.Note.Trim();
        try
        {
            // Bekleyen şikâyet doğrudan kapatılabilir; incelemedeki başka bir moderatöre aitse domain reddeder.
            report.Resolve(adminId, note, utcNow);
        }
        catch (DomainException)
        {
            return Result.Failure(AdminErrors.InvalidReportState);
        }

        await ReportQueries.ApplyTargetSideEffectsAsync(
            _dbContext, report, hideOrFlag: true, utcNow, cancellationToken);
        await _badgeAwarder.EvaluateAfterReportResolvedAsync(report.ReporterUserId, cancellationToken);

        _dbContext.AdminAuditEntries.Add(AdminActions.Entry(
            adminId, AdminActions.ReportResolve, AdminActions.ReportTarget, report.Id, note, utcNow));
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record AdminRejectReportCommand(Guid ReportId, string Note) : ICommand;

public sealed class AdminRejectReportCommandValidator : AbstractValidator<AdminRejectReportCommand>
{
    public AdminRejectReportCommandValidator()
    {
        RuleFor(command => command.ReportId).NotEmpty();
        RuleFor(command => command.Note).NotEmpty().MaximumLength(2000)
            .WithMessage(_ => ErrorMessagesResource.Admin_InvalidNote);
    }
}

/// <summary>
/// Reddetme hedefe dokunmaz: içerik başka bir şikâyet ya da yönetici tarafından gizlenmiş olabilir.
/// Gerekirse içerik ayrıca geri açılır.
/// </summary>
internal sealed class AdminRejectReportCommandHandler : ICommandHandler<AdminRejectReportCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public AdminRejectReportCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(AdminRejectReportCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } adminId)
        {
            return Result.Failure(AdminErrors.NotAuthenticated);
        }

        var report = await _dbContext.Reports
            .FirstOrDefaultAsync(candidate => candidate.Id == request.ReportId, cancellationToken);
        if (report is null)
        {
            return Result.Failure(AdminErrors.ReportNotFound);
        }

        if (report.Status is ReportStatus.Rejected)
        {
            return Result.Success();
        }

        var utcNow = _timeProvider.GetUtcNow();
        var note = request.Note.Trim();
        try
        {
            report.Reject(adminId, note, utcNow);
        }
        catch (DomainException)
        {
            return Result.Failure(AdminErrors.InvalidReportState);
        }

        _dbContext.AdminAuditEntries.Add(AdminActions.Entry(
            adminId, AdminActions.ReportReject, AdminActions.ReportTarget, report.Id, note, utcNow));
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
