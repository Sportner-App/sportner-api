using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Messaging.SearchMessages;

public sealed record SearchMessagesQuery(Guid ConversationId, string Q, int Take = 20)
    : IQuery<IReadOnlyList<MessageResponse>>;

public sealed class SearchMessagesQueryValidator : AbstractValidator<SearchMessagesQuery>
{
    public SearchMessagesQueryValidator()
    {
        RuleFor(query => query.ConversationId).NotEmpty();
        RuleFor(query => query.Q).NotEmpty().MaximumLength(100);
        RuleFor(query => query.Take).InclusiveBetween(1, 50);
    }
}

internal sealed class SearchMessagesQueryHandler
    : IQueryHandler<SearchMessagesQuery, IReadOnlyList<MessageResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public SearchMessagesQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<MessageResponse>>> Handle(
        SearchMessagesQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<IReadOnlyList<MessageResponse>>.Failure(
                MessagingErrors.NotAuthenticated);
        }

        var membership = await MessagingAccess.RequireActiveMembershipAsync(
            _dbContext,
            request.ConversationId,
            userId,
            cancellationToken);

        if (membership.IsFailure)
        {
            return Result<IReadOnlyList<MessageResponse>>.Failure(membership.Errors);
        }

        var term = request.Q.Trim().ToLowerInvariant();

        var messageIds = await _dbContext.Messages.AsNoTracking()
            .Where(message =>
                message.ConversationId == request.ConversationId
                && message.Content != null
                && message.Content.ToLower().Contains(term))
            .OrderByDescending(message => message.CreatedAt)
            .Take(request.Take)
            .Select(message => message.Id)
            .ToListAsync(cancellationToken);

        if (messageIds.Count == 0)
        {
            return Result<IReadOnlyList<MessageResponse>>.Success([]);
        }

        var messages = await _dbContext.Messages.AsNoTracking()
            .Where(message => messageIds.Contains(message.Id))
            .ToListAsync(cancellationToken);

        var responses = new List<MessageResponse>(messages.Count);
        foreach (var messageId in messageIds)
        {
            var message = messages.First(candidate => candidate.Id == messageId);
            responses.Add(await MessageMapping.ToResponseAsync(_dbContext, message, cancellationToken));
        }

        return Result<IReadOnlyList<MessageResponse>>.Success(responses);
    }
}
