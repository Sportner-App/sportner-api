using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Abstractions.Storage;
using Sportner.Application.Common.Models;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Events;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Application.Features.Admin.Content;

public sealed record AdminContentItem(
    short Type,
    Guid Id,
    string? Preview,
    string? ImageUrl,
    string State,
    DateTimeOffset CreatedAt,
    Guid? PostId,
    DateTimeOffset? EventDate);

public sealed record ListAdminUserContentQuery(Guid UserId, string? Type, int? Page, int? PageSize)
    : IQuery<PagedResult<AdminContentItem>>;

internal sealed class ListAdminUserContentQueryHandler
    : IQueryHandler<ListAdminUserContentQuery, PagedResult<AdminContentItem>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IFileStorage _fileStorage;

    public ListAdminUserContentQueryHandler(IApplicationDbContext dbContext, IFileStorage fileStorage)
    {
        _dbContext = dbContext;
        _fileStorage = fileStorage;
    }

    public async Task<Result<PagedResult<AdminContentItem>>> Handle(
        ListAdminUserContentQuery request,
        CancellationToken cancellationToken)
    {
        var type = request.Type?.Trim().ToLowerInvariant();
        if (type is not ("posts" or "comments" or "events"))
        {
            return Result<PagedResult<AdminContentItem>>.Failure(AdminErrors.InvalidContentType);
        }

        var exists = await _dbContext.Users.AsNoTracking()
            .AnyAsync(user => user.Id == request.UserId, cancellationToken);
        if (!exists)
        {
            return Result<PagedResult<AdminContentItem>>.Failure(AdminErrors.UserNotFound);
        }

        var (page, pageSize) = AdminPaging.Normalize(request.Page, request.PageSize);
        var userId = request.UserId;
        List<AdminContentItem> items;
        int total;

        switch (type)
        {
            case "posts":
            {
                var query = _dbContext.Posts.AsNoTracking().Where(post => post.UserId == userId);
                total = await query.CountAsync(cancellationToken);
                var rows = await query
                    .OrderByDescending(post => post.CreatedAt).ThenBy(post => post.Id)
                    .Skip((page - 1) * pageSize).Take(pageSize)
                    .Select(post => new { post.Id, post.Content, post.IsHidden, post.CreatedAt })
                    .ToListAsync(cancellationToken);
                var images = await AdminTargets.LoadPostImagesAsync(
                    _dbContext, _fileStorage, rows.Select(row => row.Id).ToList(), cancellationToken);
                items = rows.Select(row => new AdminContentItem(
                    (short)ReportEntityType.Post,
                    row.Id,
                    AdminTargets.Truncate(row.Content),
                    images.GetValueOrDefault(row.Id),
                    row.IsHidden ? AdminContentState.Hidden : AdminContentState.Active,
                    row.CreatedAt,
                    null,
                    null)).ToList();
                break;
            }

            case "comments":
            {
                var query = _dbContext.PostComments.AsNoTracking().Where(comment => comment.UserId == userId);
                total = await query.CountAsync(cancellationToken);
                items = (await query
                        .OrderByDescending(comment => comment.CreatedAt).ThenBy(comment => comment.Id)
                        .Skip((page - 1) * pageSize).Take(pageSize)
                        .Select(comment => new { comment.Id, comment.Content, comment.IsHidden, comment.CreatedAt, comment.PostId })
                        .ToListAsync(cancellationToken))
                    .Select(row => new AdminContentItem(
                        (short)ReportEntityType.Comment,
                        row.Id,
                        AdminTargets.Truncate(row.Content),
                        null,
                        row.IsHidden ? AdminContentState.Hidden : AdminContentState.Active,
                        row.CreatedAt,
                        row.PostId,
                        null)).ToList();
                break;
            }

            default:
            {
                var query = _dbContext.Events.AsNoTracking().Where(@event => @event.OrganizerUserId == userId);
                total = await query.CountAsync(cancellationToken);
                var rows = await query
                    .OrderByDescending(@event => @event.CreatedAt).ThenBy(@event => @event.Id)
                    .Skip((page - 1) * pageSize).Take(pageSize)
                    .Select(@event => new { @event.Id, @event.Title, @event.Status, @event.CreatedAt, @event.EventDate, @event.SportId })
                    .ToListAsync(cancellationToken);
                var covers = await AdminTargets.LoadSportCoversAsync(
                    _dbContext, rows.Select(row => row.SportId).Distinct().ToList(), cancellationToken);
                items = rows.Select(row => new AdminContentItem(
                    (short)ReportEntityType.Event,
                    row.Id,
                    AdminTargets.Truncate(row.Title),
                    covers.GetValueOrDefault(row.SportId),
                    row.Status == EventStatus.Cancelled ? AdminContentState.Cancelled : AdminContentState.Active,
                    row.CreatedAt,
                    null,
                    row.EventDate)).ToList();
                break;
            }
        }

        return Result<PagedResult<AdminContentItem>>.Success(
            PagedResult<AdminContentItem>.Create(items, page, pageSize, total));
    }
}

public sealed record SetPostHiddenCommand(Guid PostId, bool Hidden) : ICommand;

internal sealed class SetPostHiddenCommandHandler : ICommandHandler<SetPostHiddenCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public SetPostHiddenCommandHandler(IApplicationDbContext dbContext, ICurrentUser currentUser, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(SetPostHiddenCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } adminId)
        {
            return Result.Failure(AdminErrors.NotAuthenticated);
        }

        var post = await _dbContext.Posts
            .FirstOrDefaultAsync(candidate => candidate.Id == request.PostId, cancellationToken);
        if (post is null)
        {
            return Result.Failure(AdminErrors.PostNotFound);
        }

        // Zaten istenen durumdaysa değişiklik ve işlem kaydı yok (idempotent).
        if (post.IsHidden == request.Hidden)
        {
            return Result.Success();
        }

        var utcNow = _timeProvider.GetUtcNow();
        if (request.Hidden)
        {
            post.Hide(utcNow);
        }
        else
        {
            post.Unhide(utcNow);
        }

        _dbContext.AdminAuditEntries.Add(AdminActions.Entry(
            adminId,
            request.Hidden ? AdminActions.PostHide : AdminActions.PostUnhide,
            AdminActions.PostTarget,
            post.Id,
            null,
            utcNow));
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record SetCommentHiddenCommand(Guid CommentId, bool Hidden) : ICommand;

internal sealed class SetCommentHiddenCommandHandler : ICommandHandler<SetCommentHiddenCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public SetCommentHiddenCommandHandler(IApplicationDbContext dbContext, ICurrentUser currentUser, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(SetCommentHiddenCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } adminId)
        {
            return Result.Failure(AdminErrors.NotAuthenticated);
        }

        var comment = await _dbContext.PostComments
            .FirstOrDefaultAsync(candidate => candidate.Id == request.CommentId, cancellationToken);
        if (comment is null)
        {
            return Result.Failure(AdminErrors.CommentNotFound);
        }

        if (comment.IsHidden == request.Hidden)
        {
            return Result.Success();
        }

        var utcNow = _timeProvider.GetUtcNow();
        if (request.Hidden)
        {
            comment.Hide(utcNow);
        }
        else
        {
            comment.Unhide(utcNow);
        }

        _dbContext.AdminAuditEntries.Add(AdminActions.Entry(
            adminId,
            request.Hidden ? AdminActions.CommentHide : AdminActions.CommentUnhide,
            AdminActions.CommentTarget,
            comment.Id,
            null,
            utcNow));
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>
/// Etkinliği yönetici olarak iptal eder. Eylemi yapan organizatör olmadığı için
/// organizatör de katılımcılarla birlikte bildirim alır.
/// </summary>
public sealed record AdminCancelEventCommand(Guid EventId) : ICommand;

internal sealed class AdminCancelEventCommandHandler : ICommandHandler<AdminCancelEventCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly INotificationPublisher _notificationPublisher;

    public AdminCancelEventCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        INotificationPublisher notificationPublisher)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _notificationPublisher = notificationPublisher;
    }

    public async Task<Result> Handle(AdminCancelEventCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } adminId)
        {
            return Result.Failure(AdminErrors.NotAuthenticated);
        }

        var @event = await _dbContext.Events
            .Include(candidate => candidate.Participants)
            .FirstOrDefaultAsync(candidate => candidate.Id == request.EventId, cancellationToken);
        if (@event is null)
        {
            return Result.Failure(AdminErrors.EventNotFound);
        }

        // Zaten iptal edilmiş ya da tamamlanmış etkinlik 409; CancelAsync iptali sessizce geçerdi.
        if (@event.Status is EventStatus.Cancelled or EventStatus.Completed)
        {
            return Result.Failure(AdminErrors.EventNotCancellable);
        }

        var utcNow = _timeProvider.GetUtcNow();
        try
        {
            await EventCancellation.CancelAsync(
                _dbContext, _notificationPublisher, @event, adminId, utcNow, cancellationToken,
                cancelledByAdmin: true);
        }
        catch (DomainException)
        {
            return Result.Failure(AdminErrors.EventNotCancellable);
        }

        _dbContext.AdminAuditEntries.Add(AdminActions.Entry(
            adminId, AdminActions.EventCancel, AdminActions.EventTarget, @event.Id, null, utcNow));
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
