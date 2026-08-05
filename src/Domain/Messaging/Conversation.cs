using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Messaging;

public class Conversation : AggregateRoot
{
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

    public ConversationMember AddMember(Guid userId, DateTimeOffset utcNow)
    {
        EnsureOpen();

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

            existing.Rejoin(utcNow);
            Touch(utcNow);
            return existing;
        }

        var member = ConversationMember.CreateMember(Id, userId, utcNow);
        _members.Add(member);
        Touch(utcNow);

        return member;
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
}
