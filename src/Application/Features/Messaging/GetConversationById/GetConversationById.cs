using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Messaging.GetConversationById;

public sealed record GetConversationByIdQuery(Guid ConversationId) : IQuery<ConversationResponse>;

internal sealed class GetConversationByIdQueryHandler
    : IQueryHandler<GetConversationByIdQuery, ConversationResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetConversationByIdQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<ConversationResponse>> Handle(
        GetConversationByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<ConversationResponse>.Failure(MessagingErrors.NotAuthenticated);
        }

        var membership = await MessagingAccess.RequireActiveMembershipAsync(
            _dbContext,
            request.ConversationId,
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
