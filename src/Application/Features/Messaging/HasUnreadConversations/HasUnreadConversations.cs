using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Messaging.HasUnreadConversations;

public sealed record HasUnreadConversationsQuery : IQuery<bool>;

internal sealed class HasUnreadConversationsQueryHandler
    : IQueryHandler<HasUnreadConversationsQuery, bool>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public HasUnreadConversationsQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<bool>> Handle(
        HasUnreadConversationsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<bool>.Failure(MessagingErrors.NotAuthenticated);
        }

        var hasUnread = await (
            from membership in _dbContext.ConversationMembers.AsNoTracking()
            join message in _dbContext.Messages.AsNoTracking()
                on membership.ConversationId equals message.ConversationId
            where membership.UserId == userId
                && membership.LeftAt == null
                && message.SenderUserId != userId
                && (membership.LastReadAt == null || message.CreatedAt > membership.LastReadAt)
            select message.Id
        ).AnyAsync(cancellationToken);

        return Result<bool>.Success(hasUnread);
    }
}
