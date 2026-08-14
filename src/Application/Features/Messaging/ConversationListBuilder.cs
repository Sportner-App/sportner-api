using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Features.Social;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Messaging;

internal static class ConversationListBuilder
{
    private const int UnreadCap = 99;

    internal static async Task<IReadOnlyList<ConversationListItemResponse>> BuildAsync(
        IApplicationDbContext dbContext,
        Guid viewerUserId,
        IReadOnlyList<Guid> conversationIds,
        IReadOnlyDictionary<Guid, (DateTimeOffset? LastReadAt, DateTimeOffset? MutedUntil)> membershipByConversation,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (conversationIds.Count == 0)
        {
            return [];
        }

        var conversations = await dbContext.Conversations.AsNoTracking()
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

        var lastMessages = await dbContext.Messages.AsNoTracking()
            .Where(message => conversationIds.Contains(message.ConversationId))
            .GroupBy(message => message.ConversationId)
            .Select(group => new
            {
                ConversationId = group.Key,
                LastMessageAt = group.Max(message => message.CreatedAt)
            })
            .ToListAsync(cancellationToken);

        var lastMessageAtById = lastMessages.ToDictionary(
            item => item.ConversationId,
            item => item.LastMessageAt);

        var previews = await dbContext.Messages.AsNoTracking()
            .Where(message => conversationIds.Contains(message.ConversationId))
            .OrderByDescending(message => message.CreatedAt)
            .Select(message => new
            {
                message.ConversationId,
                message.CreatedAt,
                Preview = message.Content ?? message.MediaMimeType
            })
            .ToListAsync(cancellationToken);

        var previewById = previews
            .GroupBy(item => item.ConversationId)
            .ToDictionary(
                group => group.Key,
                group => group.First().Preview);

        var unreadCounts = new Dictionary<Guid, int>();
        foreach (var conversationId in conversationIds)
        {
            membershipByConversation.TryGetValue(conversationId, out var membership);
            var lastReadAt = membership.LastReadAt;

            var unreadQuery = dbContext.Messages.AsNoTracking()
                .Where(message =>
                    message.ConversationId == conversationId
                    && message.SenderUserId != viewerUserId);

            if (lastReadAt is not null)
            {
                unreadQuery = unreadQuery.Where(message => message.CreatedAt > lastReadAt);
            }

            var count = await unreadQuery.CountAsync(cancellationToken);
            unreadCounts[conversationId] = Math.Min(count, UnreadCap);
        }

        var peerRows = await (
                from member in dbContext.ConversationMembers.AsNoTracking()
                where conversationIds.Contains(member.ConversationId)
                    && member.LeftAt == null
                    && member.UserId != viewerUserId
                join profile in dbContext.UserProfiles.AsNoTracking()
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

        var peersByConversation = peerRows
            .GroupBy(row => row.ConversationId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var directPeerIds = conversations
            .Where(conversation => conversation.Type == ConversationType.Direct)
            .Select(conversation =>
            {
                if (!peersByConversation.TryGetValue(conversation.Id, out var peers)
                    || peers.Count == 0)
                {
                    return (Guid?)null;
                }

                return peers[0].UserId;
            })
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var friendIds = directPeerIds.Count == 0
            ? new HashSet<Guid>()
            : (await SocialQueries.AcceptedFriendIds(dbContext, viewerUserId)
                .Where(id => directPeerIds.Contains(id))
                .ToListAsync(cancellationToken)).ToHashSet();

        var byId = conversations.ToDictionary(conversation => conversation.Id);

        return conversationIds
            .Where(id => byId.ContainsKey(id))
            .Select(id =>
            {
                var conversation = byId[id];
                membershipByConversation.TryGetValue(id, out var membership);
                peersByConversation.TryGetValue(id, out var peers);
                var peer = conversation.Type == ConversationType.Direct
                    ? peers?.FirstOrDefault()
                    : null;

                bool? isFriend = conversation.Type == ConversationType.Direct && peer is not null
                    ? friendIds.Contains(peer.UserId)
                    : null;

                DateTimeOffset? lastMessageAt = lastMessageAtById.TryGetValue(id, out var at)
                    ? at
                    : null;
                previewById.TryGetValue(id, out var preview);

                return new ConversationListItemResponse(
                    conversation.Id,
                    (short)conversation.Type,
                    conversation.EventId,
                    conversation.Title,
                    conversation.IsClosed,
                    conversation.CreatedAt,
                    lastMessageAt,
                    preview,
                    unreadCounts.GetValueOrDefault(id),
                    membership.MutedUntil is { } mutedUntil && mutedUntil > utcNow,
                    isFriend,
                    peer?.UserId,
                    peer?.Username,
                    peer?.FirstName,
                    peer?.ProfileImageUrl);
            })
            .ToList();
    }
}
