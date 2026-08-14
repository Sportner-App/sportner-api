using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Models;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Messaging.ListMyConversations;

public sealed record ListMyConversationsQuery(int Page = 1, int PageSize = 20, short? Type = null)
    : IQuery<PagedResult<ConversationListItemResponse>>;

internal sealed class ListMyConversationsQueryHandler
    : IQueryHandler<ListMyConversationsQuery, PagedResult<ConversationListItemResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public ListMyConversationsQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
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
        var utcNow = _timeProvider.GetUtcNow();

        var memberships = await _dbContext.ConversationMembers.AsNoTracking()
            .Where(member => member.UserId == userId && member.LeftAt == null)
            .Select(member => new
            {
                member.ConversationId,
                member.LastReadAt,
                member.MutedUntil
            })
            .ToListAsync(cancellationToken);

        if (memberships.Count == 0)
        {
            return Result<PagedResult<ConversationListItemResponse>>.Success(
                PagedResult<ConversationListItemResponse>.Create(
                    [],
                    pagination.NormalizedPage,
                    pagination.NormalizedPageSize,
                    0));
        }

        var membershipById = memberships.ToDictionary(
            member => member.ConversationId,
            member => (member.LastReadAt, member.MutedUntil));

        var myConversationIds = memberships.Select(member => member.ConversationId).ToList();

        var orderedIds = await _dbContext.Conversations.AsNoTracking()
            .Where(conversation =>
                myConversationIds.Contains(conversation.Id)
                && (request.Type == null || (short)conversation.Type == request.Type.Value))
            .Select(conversation => new
            {
                conversation.Id,
                conversation.CreatedAt,
                LastMessageAt = _dbContext.Messages
                    .Where(message => message.ConversationId == conversation.Id)
                    .OrderByDescending(message => message.CreatedAt)
                    .Select(message => (DateTimeOffset?)message.CreatedAt)
                    .FirstOrDefault()
            })
            .OrderByDescending(item => item.LastMessageAt ?? item.CreatedAt)
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);

        var total = orderedIds.Count;
        var pageIds = orderedIds
            .Skip(pagination.Skip)
            .Take(pagination.NormalizedPageSize)
            .ToList();

        var items = await ConversationListBuilder.BuildAsync(
            _dbContext,
            userId,
            pageIds,
            membershipById,
            utcNow,
            cancellationToken);

        var byId = items.ToDictionary(item => item.Id);
        var orderedItems = pageIds
            .Where(id => byId.ContainsKey(id))
            .Select(id => byId[id])
            .ToList();

        return Result<PagedResult<ConversationListItemResponse>>.Success(
            PagedResult<ConversationListItemResponse>.Create(
                orderedItems,
                pagination.NormalizedPage,
                pagination.NormalizedPageSize,
                total));
    }
}
