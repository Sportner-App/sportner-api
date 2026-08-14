using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Messaging.SearchMyConversations;

public sealed record SearchMyConversationsQuery(string Q, int Take = 20)
    : IQuery<IReadOnlyList<ConversationListItemResponse>>;

public sealed class SearchMyConversationsQueryValidator
    : AbstractValidator<SearchMyConversationsQuery>
{
    public SearchMyConversationsQueryValidator()
    {
        RuleFor(query => query.Q).NotEmpty().MaximumLength(50);
        RuleFor(query => query.Take).InclusiveBetween(1, 50);
    }
}

internal sealed class SearchMyConversationsQueryHandler
    : IQueryHandler<SearchMyConversationsQuery, IReadOnlyList<ConversationListItemResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public SearchMyConversationsQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<IReadOnlyList<ConversationListItemResponse>>> Handle(
        SearchMyConversationsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<IReadOnlyList<ConversationListItemResponse>>.Failure(
                MessagingErrors.NotAuthenticated);
        }

        var term = request.Q.Trim().ToLowerInvariant();
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
            return Result<IReadOnlyList<ConversationListItemResponse>>.Success([]);
        }

        var conversationIds = memberships.Select(member => member.ConversationId).ToList();

        var conversations = await _dbContext.Conversations.AsNoTracking()
            .Where(conversation => conversationIds.Contains(conversation.Id))
            .Select(conversation => new
            {
                conversation.Id,
                conversation.Type,
                conversation.EventId,
                conversation.Title,
                conversation.IsClosed,
                conversation.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var peerRows = await (
                from member in _dbContext.ConversationMembers.AsNoTracking()
                where conversationIds.Contains(member.ConversationId)
                    && member.LeftAt == null
                    && member.UserId != userId
                join profile in _dbContext.UserProfiles.AsNoTracking()
                    on member.UserId equals profile.UserId into profiles
                from profile in profiles.DefaultIfEmpty()
                select new
                {
                    member.ConversationId,
                    member.UserId,
                    Username = profile != null ? profile.Username : null,
                    FirstName = profile != null ? profile.FirstName : null,
                    ProfileImageUrl = profile != null ? profile.ProfileImageUrl : null
                })
            .ToListAsync(cancellationToken);

        var matchedIds = conversations
            .Where(conversation =>
            {
                if (conversation.Title is not null
                    && conversation.Title.ToLowerInvariant().Contains(term))
                {
                    return true;
                }

                return peerRows.Any(peer =>
                    peer.ConversationId == conversation.Id
                    && ((peer.Username is not null && peer.Username.ToLowerInvariant().Contains(term))
                        || (peer.FirstName is not null
                            && peer.FirstName.ToLowerInvariant().Contains(term))));
            })
            .Select(conversation => conversation.Id)
            .Take(request.Take)
            .ToList();

        if (matchedIds.Count == 0)
        {
            return Result<IReadOnlyList<ConversationListItemResponse>>.Success([]);
        }

        var items = await ConversationListBuilder.BuildAsync(
            _dbContext,
            userId,
            matchedIds,
            memberships.ToDictionary(
                member => member.ConversationId,
                member => (member.LastReadAt, member.MutedUntil)),
            utcNow,
            cancellationToken);

        return Result<IReadOnlyList<ConversationListItemResponse>>.Success(items);
    }
}
