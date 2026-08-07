using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Models;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Messaging.ListMessages;

/// <summary>
/// Returns messages in chronological order. Pass <paramref name="Before"/> to page older
/// history; omit it to load the latest page.
/// </summary>
public sealed record ListMessagesQuery(Guid ConversationId, string? Before = null, int Limit = 30)
    : IQuery<CursorPagedResult<MessageResponse>>;

internal sealed class ListMessagesQueryHandler
    : IQueryHandler<ListMessagesQuery, CursorPagedResult<MessageResponse>>
{
    private const int MaxLimit = 100;
    private const int DefaultLimit = 30;

    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListMessagesQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<CursorPagedResult<MessageResponse>>> Handle(
        ListMessagesQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<CursorPagedResult<MessageResponse>>.Failure(
                MessagingErrors.NotAuthenticated);
        }

        var membership = await MessagingAccess.RequireActiveMembershipAsync(
            _dbContext,
            request.ConversationId,
            userId,
            cancellationToken);

        if (membership.IsFailure)
        {
            return Result<CursorPagedResult<MessageResponse>>.Failure(membership.Errors);
        }

        var limit = request.Limit switch
        {
            < 1 => DefaultLimit,
            > MaxLimit => MaxLimit,
            _ => request.Limit
        };

        var query = _dbContext.Messages.AsNoTracking()
            .Where(message => message.ConversationId == request.ConversationId);

        if (!string.IsNullOrWhiteSpace(request.Before))
        {
            if (!MessageCursor.TryDecode(request.Before, out var beforeCreatedAt, out var beforeId))
            {
                return Result<CursorPagedResult<MessageResponse>>.Failure(
                    MessagingErrors.InvalidCursor);
            }

            // Keyset: strictly older than (CreatedAt, Id). Guid string compare is EF-translatable.
            var beforeIdKey = beforeId.ToString("D");
            query = query.Where(message =>
                message.CreatedAt < beforeCreatedAt
                || (message.CreatedAt == beforeCreatedAt
                    && message.Id.ToString().CompareTo(beforeIdKey) < 0));
        }

        // Fetch newest-first page, then reverse to chronological for the client.
        var page = await (
                from message in query
                join profile in _dbContext.Profiles.AsNoTracking()
                    on message.SenderUserId equals profile.UserId into profiles
                from profile in profiles.DefaultIfEmpty()
                orderby message.CreatedAt descending, message.Id descending
                select new
                {
                    Message = message,
                    Username = profile != null ? profile.Username : null,
                    FirstName = profile != null ? profile.FirstName : null
                })
            .Take(limit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = page.Count > limit;
        var window = hasMore ? page.Take(limit).ToList() : page;
        window.Reverse();

        var items = window
            .Select(row => MessageMapping.ToResponse(row.Message, row.Username, row.FirstName))
            .ToList();

        string? nextCursor = null;

        if (hasMore && items.Count > 0)
        {
            var oldest = items[0];
            nextCursor = MessageCursor.Encode(oldest.CreatedAt, oldest.Id);
        }

        return Result<CursorPagedResult<MessageResponse>>.Success(
            CursorPagedResult<MessageResponse>.Create(items, nextCursor));
    }
}
