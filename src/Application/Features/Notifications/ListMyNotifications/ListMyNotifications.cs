using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Models;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Social;

namespace Sportner.Application.Features.Notifications.ListMyNotifications;

public sealed record ListMyNotificationsQuery(
    bool UnreadOnly = false,
    string? Before = null,
    int Limit = 30) : IQuery<CursorPagedResult<NotificationResponse>>;

internal sealed class ListMyNotificationsQueryHandler
    : IQueryHandler<ListMyNotificationsQuery, CursorPagedResult<NotificationResponse>>
{
    private const int MaxLimit = 100;

    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListMyNotificationsQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<CursorPagedResult<NotificationResponse>>> Handle(
        ListMyNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<CursorPagedResult<NotificationResponse>>.Failure(
                NotificationErrors.NotAuthenticated);
        }

        var limit = request.Limit is < 1 or > MaxLimit ? 30 : request.Limit;

        var query =
            from notification in _dbContext.Notifications.AsNoTracking()
            join actor in _dbContext.UserProfiles.AsNoTracking()
                on notification.ActorUserId equals actor.UserId into actors
            from actor in actors.DefaultIfEmpty()
            where notification.RecipientUserId == userId
            select new { notification, actor };

        var blockedIds = BlockQueries.BlockedUserIds(_dbContext, userId);
        query = query.Where(row =>
            row.notification.ActorUserId == null
            || !blockedIds.Contains(row.notification.ActorUserId.Value));

        if (request.UnreadOnly)
        {
            query = query.Where(row => !row.notification.IsRead);
        }

        if (!string.IsNullOrWhiteSpace(request.Before))
        {
            if (!Guid.TryParse(request.Before, out var beforeId))
            {
                return Result<CursorPagedResult<NotificationResponse>>.Failure(
                    NotificationErrors.InvalidCursor);
            }

            var cursor = await _dbContext.Notifications.AsNoTracking()
                .Where(notification =>
                    notification.Id == beforeId && notification.RecipientUserId == userId)
                .Select(notification => new { notification.CreatedAt, notification.Id })
                .FirstOrDefaultAsync(cancellationToken);

            if (cursor is null)
            {
                return Result<CursorPagedResult<NotificationResponse>>.Failure(
                    NotificationErrors.InvalidCursor);
            }

            var cursorIdKey = cursor.Id.ToString("D");
            query = query.Where(row =>
                row.notification.CreatedAt < cursor.CreatedAt
                || (row.notification.CreatedAt == cursor.CreatedAt
                    && row.notification.Id.ToString().CompareTo(cursorIdKey) < 0));
        }

        var page = await query
            .OrderByDescending(row => row.notification.CreatedAt)
            .ThenByDescending(row => row.notification.Id)
            .Take(limit + 1)
            .Select(row => new NotificationResponse(
                row.notification.Id,
                (short)row.notification.NotificationType,
                (short)row.notification.EntityType,
                row.notification.EntityId,
                row.notification.ActorUserId,
                row.actor != null ? row.actor.Username : null,
                row.notification.Title,
                row.notification.Body,
                row.notification.IsRead,
                row.notification.ReadAt,
                row.notification.CreatedAt))
            .ToListAsync(cancellationToken);

        var hasMore = page.Count > limit;
        var items = hasMore ? page.Take(limit).ToList() : page;
        var nextCursor = hasMore && items.Count > 0 ? items[^1].Id.ToString("D") : null;

        return Result<CursorPagedResult<NotificationResponse>>.Success(
            CursorPagedResult<NotificationResponse>.Create(items, nextCursor));
    }
}
