using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Messaging;

public class Conversation : AggregateRoot
{
    public const int MaxGroupMembers = 50;

    private readonly List<ConversationMember> _members = [];

    private Conversation()
    {
    }

    public ConversationType Type { get; private set; }

    public Guid? EventId { get; private set; }

    public string? Title { get; private set; }

    public bool IsClosed { get; private set; }

    public DateTimeOffset? ClosedAt { get; private set; }

    public IReadOnlyCollection<ConversationMember> Members => _members.AsReadOnly();

    public static Conversation CreateEventConversation(
        Guid eventId,
        Guid ownerUserId,
        DateTimeOffset utcNow,
        string? title = null)
    {
        if (eventId == Guid.Empty)
        {
            throw new DomainException("Event id is required.");
        }

        if (ownerUserId == Guid.Empty)
        {
            throw new DomainException("Owner user id is required.");
        }

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Type = ConversationType.Event,
            EventId = eventId,
            Title = NormalizeOptionalTitle(title),
            IsClosed = false,
            ClosedAt = null,
            CreatedAt = utcNow
        };

        conversation._members.Add(
            ConversationMember.CreateOwner(conversation.Id, ownerUserId, utcNow));

        return conversation;
    }

    public static Conversation CreateDirectConversation(
        Guid creatorUserId,
        Guid otherUserId,
        DateTimeOffset utcNow)
    {
        if (creatorUserId == Guid.Empty || otherUserId == Guid.Empty)
        {
            throw new DomainException("Both participants are required.");
        }

        if (creatorUserId == otherUserId)
        {
            throw new DomainException("Cannot create a direct conversation with yourself.");
        }

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Type = ConversationType.Direct,
            EventId = null,
            Title = null,
            IsClosed = false,
            ClosedAt = null,
            CreatedAt = utcNow
        };

        conversation._members.Add(
            ConversationMember.CreateOwner(conversation.Id, creatorUserId, utcNow));
        conversation._members.Add(
            ConversationMember.CreateMember(conversation.Id, otherUserId, utcNow));

        return conversation;
    }

    public static Conversation CreateGroupConversation(
        Guid ownerUserId,
        string title,
        IReadOnlyCollection<Guid> memberUserIds,
        DateTimeOffset utcNow)
    {
        if (ownerUserId == Guid.Empty)
        {
            throw new DomainException("Owner user id is required.");
        }

        var normalizedTitle = NormalizeRequiredTitle(title);
        var uniqueMembers = memberUserIds
            .Where(id => id != Guid.Empty && id != ownerUserId)
            .Distinct()
            .ToList();

        if (uniqueMembers.Count + 1 > MaxGroupMembers)
        {
            throw new DomainException(
                $"A group conversation may have at most {MaxGroupMembers} members.");
        }

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Type = ConversationType.Group,
            EventId = null,
            Title = normalizedTitle,
            IsClosed = false,
            ClosedAt = null,
            CreatedAt = utcNow
        };

        conversation._members.Add(
            ConversationMember.CreateOwner(conversation.Id, ownerUserId, utcNow));

        foreach (var memberUserId in uniqueMembers)
        {
            conversation._members.Add(
                ConversationMember.CreateMember(conversation.Id, memberUserId, utcNow));
        }

        return conversation;
    }

    public ConversationMember AddMember(Guid userId, DateTimeOffset utcNow)
    {
        EnsureOpen();

        if (Type is ConversationType.Direct)
        {
            throw new DomainException("Direct conversations have a fixed membership.");
        }

        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        var existing = FindMemberOrDefault(userId);

        if (existing is not null)
        {
            if (existing.IsActive())
            {
                throw new DomainException("User is already an active conversation member.");
            }

            if (Type is ConversationType.Group && ActiveMemberCount() >= MaxGroupMembers)
            {
                throw new DomainException(
                    $"A group conversation may have at most {MaxGroupMembers} members.");
            }

            existing.Rejoin(utcNow);
            Touch(utcNow);
            return existing;
        }

        if (Type is ConversationType.Group && ActiveMemberCount() >= MaxGroupMembers)
        {
            throw new DomainException(
                $"A group conversation may have at most {MaxGroupMembers} members.");
        }

        var member = ConversationMember.CreateMember(Id, userId, utcNow);
        _members.Add(member);
        Touch(utcNow);

        return member;
    }

    public void InviteMember(Guid invitedByUserId, Guid userId, DateTimeOffset utcNow)
    {
        EnsureOpen();

        if (Type is not ConversationType.Group)
        {
            throw new DomainException("Only group conversations support invites.");
        }

        if (!IsOwnerOrModerator(invitedByUserId))
        {
            throw new DomainException("Only owners or moderators can invite members.");
        }

        AddMember(userId, utcNow);
    }

    public void Leave(Guid userId, DateTimeOffset utcNow)
    {
        EnsureOpen();

        if (Type is ConversationType.Event)
        {
            throw new DomainException("Event conversation membership is managed by the event.");
        }

        var member = FindMember(userId);
        member.Leave(utcNow);
        Touch(utcNow);
    }

    public void RemoveMember(Guid userId, DateTimeOffset utcNow)
    {
        EnsureOpen();

        var member = FindMember(userId);

        if (member.Role is ConversationMemberRole.Owner)
        {
            throw new DomainException("Owner cannot be removed from the conversation.");
        }

        member.Leave(utcNow);
        Touch(utcNow);
    }

    public int ActiveMemberCount() => _members.Count(member => member.IsActive());

    public bool ContainsActiveMember(Guid userId)
    {
        var member = FindMemberOrDefault(userId);
        return member is not null && member.IsActive();
    }

    public bool CanUserSendMessage(Guid userId)
    {
        if (IsClosed)
        {
            return false;
        }

        var member = FindMemberOrDefault(userId);
        return member is not null && member.CanSendMessages();
    }

    public bool IsOwner(Guid userId)
    {
        var member = FindMemberOrDefault(userId);
        return member is not null
            && member.IsActive()
            && member.Role is ConversationMemberRole.Owner;
    }

    public bool IsOwnerOrModerator(Guid userId)
    {
        var member = FindMemberOrDefault(userId);
        return member is not null
            && member.IsActive()
            && member.Role is ConversationMemberRole.Owner or ConversationMemberRole.Moderator;
    }

    public void PromoteMemberToModerator(Guid userId, DateTimeOffset utcNow)
    {
        EnsureOpen();

        var member = FindMember(userId);
        member.PromoteToModerator(utcNow);
        Touch(utcNow);
    }

    public void DemoteModerator(Guid userId, DateTimeOffset utcNow)
    {
        EnsureOpen();

        var member = FindMember(userId);
        member.DemoteToMember(utcNow);
        Touch(utcNow);
    }

    public void Close(DateTimeOffset utcNow)
    {
        if (IsClosed)
        {
            return;
        }

        IsClosed = true;
        ClosedAt = utcNow;
        Touch(utcNow);
    }

    private ConversationMember FindMember(Guid userId)
    {
        var member = FindMemberOrDefault(userId);

        if (member is null)
        {
            throw new DomainException("Conversation member was not found.");
        }

        if (member.ConversationId != Id)
        {
            throw new DomainException("Member does not belong to this conversation.");
        }

        return member;
    }

    private ConversationMember? FindMemberOrDefault(Guid userId)
    {
        return _members.FirstOrDefault(member => member.UserId == userId);
    }

    private void EnsureOpen()
    {
        if (IsClosed)
        {
            throw new DomainException("Conversation is closed.");
        }
    }

    private void Touch(DateTimeOffset utcNow)
    {
        UpdatedAt = utcNow;
    }

    private static string? NormalizeOptionalTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        var normalized = title.Trim();

        if (normalized.Length > 100)
        {
            throw new DomainException("Conversation title cannot exceed 100 characters.");
        }

        return normalized;
    }

    private static string NormalizeRequiredTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Group conversation title is required.");
        }

        var normalized = title.Trim();

        if (normalized.Length > 100)
        {
            throw new DomainException("Conversation title cannot exceed 100 characters.");
        }

        return normalized;
    }
}
