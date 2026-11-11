using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Models;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Admin.Audit;

public sealed record AdminAuditItem(
    Guid Id,
    DateTimeOffset CreatedAt,
    string Action,
    string TargetType,
    Guid TargetId,
    string? TargetLabel,
    string? Details,
    Guid ActorUserId,
    string? ActorUsername);

public sealed record ListAdminAuditQuery(int? Page, int? PageSize) : IQuery<PagedResult<AdminAuditItem>>;

internal sealed class ListAdminAuditQueryHandler : IQueryHandler<ListAdminAuditQuery, PagedResult<AdminAuditItem>>
{
    private readonly IApplicationDbContext _dbContext;

    public ListAdminAuditQueryHandler(IApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<PagedResult<AdminAuditItem>>> Handle(ListAdminAuditQuery request, CancellationToken cancellationToken)
    {
        var (page, pageSize) = AdminPaging.Normalize(request.Page, request.PageSize);
        var total = await _dbContext.AdminAuditEntries.CountAsync(cancellationToken);
        var rows = await _dbContext.AdminAuditEntries.AsNoTracking()
            .OrderByDescending(entry => entry.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Kullanıcı hedefleri ve işlemi yapan yönetici için kullanıcı adları tek sorguda.
        var ids = rows.Select(row => row.ActorUserId)
            .Concat(rows.Where(row => row.TargetType == AdminActions.UserTarget).Select(row => row.TargetId))
            .Distinct()
            .ToList();
        var names = await _dbContext.UserProfiles.AsNoTracking()
            .Where(profile => ids.Contains(profile.UserId))
            .ToDictionaryAsync(profile => profile.UserId, profile => profile.Username, cancellationToken);

        var items = rows.Select(row => new AdminAuditItem(
                row.Id,
                row.CreatedAt,
                row.Action,
                row.TargetType,
                row.TargetId,
                row.TargetType == AdminActions.UserTarget ? names.GetValueOrDefault(row.TargetId) : null,
                row.Details,
                row.ActorUserId,
                names.GetValueOrDefault(row.ActorUserId)))
            .ToList();

        return Result<PagedResult<AdminAuditItem>>.Success(
            PagedResult<AdminAuditItem>.Create(items, page, pageSize, total));
    }
}
