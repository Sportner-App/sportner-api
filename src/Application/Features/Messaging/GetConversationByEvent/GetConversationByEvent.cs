using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Messaging.GetConversationByEvent;

public sealed record GetConversationByEventQuery(Guid EventId) : IQuery<ConversationResponse>;

internal sealed class GetConversationByEventQueryHandler
    : IQueryHandler<GetConversationByEventQuery, ConversationResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetConversationByEventQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<ConversationResponse>> Handle(
        GetConversationByEventQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<ConversationResponse>.Failure(MessagingErrors.NotAuthenticated);
        }

        var conversationId = await _dbContext.Conversations.AsNoTracking()
            .Where(conversation =>
                conversation.EventId == request.EventId
                && conversation.Type == ConversationType.Event)
            .Select(conversation => (Guid?)conversation.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (conversationId is null)
        {
            return Result<ConversationResponse>.Failure(MessagingErrors.ConversationNotFound);
        }

        var membership = await MessagingAccess.RequireActiveMembershipAsync(
            _dbContext,
            conversationId.Value,
            userId,
            cancellationToken);

        if (membership.IsFailure)
        {
            return Result<ConversationResponse>.Failure(membership.Errors);
        }

        var response = await MessagingAccess.BuildConversationResponseAsync(
            _dbContext,
            membership.Value!,
            userId,
            cancellationToken);

        return Result<ConversationResponse>.Success(response!);
    }
}
