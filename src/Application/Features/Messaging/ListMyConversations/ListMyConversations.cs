using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Models;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Messaging.ListMyConversations;

public sealed record ListMyConversationsQuery(int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<ConversationListItemResponse>>;

internal sealed class ListMyConversationsQueryHandler
    : IQueryHandler<ListMyConversationsQuery, PagedResult<ConversationListItemResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListMyConversationsQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<ConversationListItemResponse>>> Handle(
        ListMyConversationsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<PagedResult<ConversationListItemResponse>>.Failure(
                MessagingErrors.NotAuthenticated);
        }

        var pagination = new PaginationRequest(request.Page, request.PageSize);

        var conversationIds = _dbContext.ConversationMembers.AsNoTracking()
            .Where(member => member.UserId == userId && member.LeftAt == null)
            .Select(member => member.ConversationId);

        var query =
            from conversation in _dbContext.Conversations.AsNoTracking()
            where conversationIds.Contains(conversation.Id)
                && conversation.Type == ConversationType.Event
            select new ConversationListItemResponse(
                conversation.Id,
                (short)conversation.Type,
                conversation.EventId,
                conversation.Title,
                conversation.IsClosed,
                conversation.CreatedAt,
                _dbContext.Messages
                    .Where(message => message.ConversationId == conversation.Id)
                    .OrderByDescending(message => message.CreatedAt)
                    .Select(message => (DateTimeOffset?)message.CreatedAt)
                    .FirstOrDefault(),
                _dbContext.Messages
                    .Where(message => message.ConversationId == conversation.Id)
                    .OrderByDescending(message => message.CreatedAt)
                    .Select(message => message.Content ?? message.MediaMimeType)
                    .FirstOrDefault());

        query = query.OrderByDescending(item => item.LastMessageAt ?? item.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip(pagination.Skip)
            .Take(pagination.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        return Result<PagedResult<ConversationListItemResponse>>.Success(
            PagedResult<ConversationListItemResponse>.Create(
                items,
                pagination.NormalizedPage,
                pagination.NormalizedPageSize,
                total));
    }
}
