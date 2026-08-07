using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Events;
using Sportner.Domain.Messaging;
using Sportner.Domain.Users;

namespace Sportner.Application.Features.Events;

internal static class EventAccess
{
    internal static async Task<Result<(User User, Domain.Events.Event Event)>> LoadOrganizerEventAsync(
        IApplicationDbContext dbContext,
        Guid userId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        if (user is null)
        {
            return Result<(User, Domain.Events.Event)>.Failure(EventErrors.UserNotFound);
        }

        var @event = await LoadAggregateAsync(dbContext, eventId, cancellationToken);

        if (@event is null)
        {
            return Result<(User, Domain.Events.Event)>.Failure(EventErrors.NotFound);
        }

        if (@event.OrganizerUserId != userId)
        {
            return Result<(User, Domain.Events.Event)>.Failure(EventErrors.NotOrganizer);
        }

        return Result<(User, Domain.Events.Event)>.Success((user, @event));
    }

    internal static Task<Domain.Events.Event?> LoadAggregateAsync(
        IApplicationDbContext dbContext,
        Guid eventId,
        CancellationToken cancellationToken) =>
        dbContext.Events
            .Include(candidate => candidate.Participants)
            .Include(candidate => candidate.Waitlist)
            .FirstOrDefaultAsync(candidate => candidate.Id == eventId, cancellationToken);

    internal static Task<Conversation?> FindEventConversationAsync(
        IApplicationDbContext dbContext,
        Guid eventId,
        CancellationToken cancellationToken) =>
        dbContext.Conversations
            .Include(conversation => conversation.Members)
            .FirstOrDefaultAsync(
                conversation =>
                    conversation.EventId == eventId
                    && conversation.Type == ConversationType.Event,
                cancellationToken);

    internal static async Task EnsureEventConversationAsync(
        IApplicationDbContext dbContext,
        Domain.Events.Event @event,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var existing = await FindEventConversationAsync(dbContext, @event.Id, cancellationToken);

        if (existing is not null)
        {
            return;
        }

        dbContext.Conversations.Add(
            Conversation.CreateEventConversation(@event.Id, @event.OrganizerUserId, utcNow, @event.Title));
    }

    internal static async Task AddConversationMemberIfPresentAsync(
        IApplicationDbContext dbContext,
        Guid eventId,
        Guid userId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var conversation = await FindEventConversationAsync(dbContext, eventId, cancellationToken);

        if (conversation is null || conversation.IsClosed)
        {
            return;
        }

        if (conversation.ContainsActiveMember(userId))
        {
            return;
        }

        conversation.AddMember(userId, utcNow);
    }

    internal static async Task RemoveConversationMemberIfPresentAsync(
        IApplicationDbContext dbContext,
        Guid eventId,
        Guid userId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var conversation = await FindEventConversationAsync(dbContext, eventId, cancellationToken);

        if (conversation is null || conversation.IsClosed)
        {
            return;
        }

        if (!conversation.ContainsActiveMember(userId))
        {
            return;
        }

        conversation.RemoveMember(userId, utcNow);
    }

    internal static async Task CloseEventConversationAsync(
        IApplicationDbContext dbContext,
        Guid eventId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var conversation = await FindEventConversationAsync(dbContext, eventId, cancellationToken);

        conversation?.Close(utcNow);
    }
}
