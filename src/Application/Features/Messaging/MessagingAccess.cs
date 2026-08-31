using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Social;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Events;
using Sportner.Domain.Messaging;

namespace Sportner.Application.Features.Messaging;

internal static class MessagingAccess
{
    internal static async Task<Result<Conversation>> RequireActiveMembershipAsync(
        IApplicationDbContext dbContext,
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var conversation = await dbContext.Conversations
            .Include(candidate => candidate.Members)
            .FirstOrDefaultAsync(candidate => candidate.Id == conversationId, cancellationToken);

        if (conversation is null)
        {
            return Result<Conversation>.Failure(MessagingErrors.ConversationNotFound);
        }

        if (!conversation.ContainsActiveMember(userId))
        {
            return Result<Conversation>.Failure(MessagingErrors.NotMember);
        }

        return Result<Conversation>.Success(conversation);
    }

    internal static async Task CloseIfEventEndedAsync(
        IApplicationDbContext dbContext,
        Conversation conversation,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (conversation.IsClosed
            || conversation.Type is not ConversationType.Event
            || conversation.EventId is not { } eventId)
        {
            return;
        }

        if (await IsLinkedEventEndedAsync(dbContext, eventId, utcNow, cancellationToken))
        {
            conversation.Close(utcNow);
        }
    }

    internal static async Task<bool> IsLinkedEventEndedAsync(
        IApplicationDbContext dbContext,
        Guid eventId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var snapshot = await dbContext.Events.AsNoTracking()
            .Where(@event => @event.Id == eventId)
            .Select(@event => new
            {
                @event.Status,
                @event.EventDate,
                @event.DurationMinutes
            })
            .FirstOrDefaultAsync(cancellationToken);

        return snapshot is not null
            && Event.HasEnded(
                snapshot.Status,
                snapshot.EventDate,
                snapshot.DurationMinutes,
                utcNow);
    }

    internal static async Task<IReadOnlySet<Guid>> ListEndedEventIdsAsync(
        IApplicationDbContext dbContext,
        IReadOnlyCollection<Guid> eventIds,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (eventIds.Count == 0)
        {
            return new HashSet<Guid>();
        }

        var snapshots = await dbContext.Events.AsNoTracking()
            .Where(@event => eventIds.Contains(@event.Id))
            .Select(@event => new
            {
                @event.Id,
                @event.Status,
                @event.EventDate,
                @event.DurationMinutes
            })
            .ToListAsync(cancellationToken);

        return snapshots
            .Where(snapshot => Event.HasEnded(
                snapshot.Status,
                snapshot.EventDate,
                snapshot.DurationMinutes,
                utcNow))
            .Select(snapshot => snapshot.Id)
            .ToHashSet();
    }

    internal static async Task<bool> IsDirectPeerBlockedAsync(
        IApplicationDbContext dbContext,
        Conversation conversation,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (conversation.Type is not ConversationType.Direct)
        {
            return false;
        }

        var peerId = conversation.Members
            .Where(member => member.IsActive() && member.UserId != userId)
            .Select(member => member.UserId)
            .FirstOrDefault();

        if (peerId == Guid.Empty)
        {
            return false;
        }

        return await BlockQueries.BlockedPairExistsAsync(
            dbContext,
            userId,
            peerId,
            cancellationToken);
    }

    internal static async Task<ConversationResponse?> BuildConversationResponseAsync(
        IApplicationDbContext dbContext,
        Conversation conversation,
        Guid viewerUserId,
        CancellationToken cancellationToken)
    {
        var myMembership = conversation.Members
            .FirstOrDefault(member => member.UserId == viewerUserId && member.IsActive());

        if (myMembership is null)
        {
            return null;
        }

        var activeMemberIds = conversation.Members
            .Where(member => member.IsActive())
            .Select(member => member.UserId)
            .ToList();

        var profiles = await dbContext.UserProfiles.AsNoTracking()
            .Where(profile => activeMemberIds.Contains(profile.UserId))
            .Select(profile => new
            {
                profile.UserId,
                profile.Username,
                profile.FirstName,
                profile.ProfileImageUrl
            })
            .ToListAsync(cancellationToken);

        var profileByUserId = profiles.ToDictionary(profile => profile.UserId);

        var members = conversation.Members
            .Where(member => member.IsActive())
            .OrderBy(member => member.JoinedAt)
            .Select(member =>
            {
                profileByUserId.TryGetValue(member.UserId, out var profile);

                return new ConversationMemberResponse(
                    member.UserId,
                    profile?.Username,
                    profile?.FirstName,
                    profile?.ProfileImageUrl,
                    (short)member.Role,
                    member.JoinedAt,
                    member.LastReadAt,
                    member.LastReadMessageId);
            })
            .ToList();

        var isClosed = conversation.IsClosed;
        if (!isClosed
            && conversation.Type is ConversationType.Event
            && conversation.EventId is { } eventId)
        {
            isClosed = await IsLinkedEventEndedAsync(
                dbContext,
                eventId,
                DateTimeOffset.UtcNow,
                cancellationToken);
        }

        return new ConversationResponse(
            conversation.Id,
            (short)conversation.Type,
            conversation.EventId,
            conversation.Title,
            isClosed,
            conversation.ClosedAt,
            conversation.CreatedAt,
            (short)myMembership.Role,
            members,
            myMembership.MutedUntil,
            myMembership.LastReadMessageId,
            myMembership.LastReadAt);
    }

    internal static async Task<Conversation?> FindDirectBetweenAsync(
        IApplicationDbContext dbContext,
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken cancellationToken)
    {
        var myConversationIds = await dbContext.ConversationMembers.AsNoTracking()
            .Where(member => member.UserId == firstUserId && member.LeftAt == null)
            .Select(member => member.ConversationId)
            .ToListAsync(cancellationToken);

        if (myConversationIds.Count == 0)
        {
            return null;
        }

        return await dbContext.Conversations
            .Include(conversation => conversation.Members)
            .Where(conversation =>
                conversation.Type == Domain.Common.Enums.ConversationType.Direct
                && myConversationIds.Contains(conversation.Id)
                && conversation.Members.Any(member =>
                    member.UserId == secondUserId && member.LeftAt == null))
            .FirstOrDefaultAsync(cancellationToken);
    }
}

internal static class MessageCursor
{
    private const char Separator = '|';

    internal static string Encode(DateTimeOffset createdAt, Guid id)
    {
        var raw =
            $"{createdAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)}{Separator}{id:N}";

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    internal static bool TryDecode(string? cursor, out DateTimeOffset createdAt, out Guid id)
    {
        createdAt = default;
        id = default;

        if (string.IsNullOrWhiteSpace(cursor))
        {
            return false;
        }

        try
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var parts = raw.Split(Separator, 2);

            if (parts.Length != 2)
            {
                return false;
            }

            if (!DateTimeOffset.TryParse(
                    parts[0],
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out createdAt))
            {
                return false;
            }

            return Guid.TryParseExact(parts[1], "N", out id);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
