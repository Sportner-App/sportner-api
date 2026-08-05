using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Messaging;

public class ConversationMember : AuditableEntity
{
    private ConversationMember()
    {
    }

    public Guid ConversationId { get; private set; }

    public Guid UserId { get; private set; }

    public ConversationMemberRole Role { get; private set; }

    public DateTimeOffset JoinedAt { get; private set; }

    public DateTimeOffset? LeftAt { get; private set; }

    public static ConversationMember CreateOwner(
        Guid conversationId,
        Guid userId,
        DateTimeOffset utcNow)
    {
        return Create(conversationId, userId, ConversationMemberRole.Owner, utcNow);
    }

    public static ConversationMember CreateMember(
        Guid conversationId,
        Guid userId,
        DateTimeOffset utcNow)
    {
        return Create(conversationId, userId, ConversationMemberRole.Member, utcNow);
    }

    public void PromoteToModerator(DateTimeOffset utcNow)
    {
        EnsureActive();

        if (Role is ConversationMemberRole.Owner)
        {
            throw new DomainException("Owner cannot be promoted to moderator.");
        }

        if (Role is ConversationMemberRole.Moderator)
        {
            return;
        }

        Role = ConversationMemberRole.Moderator;
        Touch(utcNow);
    }

    public void DemoteToMember(DateTimeOffset utcNow)
    {
        EnsureActive();

        if (Role is ConversationMemberRole.Owner)
        {
            throw new DomainException("Owner cannot be demoted.");
        }

        if (Role is ConversationMemberRole.Member)
        {
            return;
        }

        Role = ConversationMemberRole.Member;
        Touch(utcNow);
    }

    public void Leave(DateTimeOffset utcNow)
    {
        if (!IsActive())
        {
            return;
        }

        if (Role is ConversationMemberRole.Owner)
        {
            throw new DomainException("Owner cannot leave the conversation.");
        }

        LeftAt = utcNow;
        Touch(utcNow);
    }

    public void Rejoin(DateTimeOffset utcNow)
    {
        if (IsActive())
        {
            throw new DomainException("Member is already active.");
        }

        if (Role is ConversationMemberRole.Owner)
        {
            throw new DomainException("Owner membership cannot become inactive.");
        }

        Role = ConversationMemberRole.Member;
        LeftAt = null;
        JoinedAt = utcNow;
        Touch(utcNow);
    }

    public bool IsActive()
    {
        return LeftAt is null;
    }

    public bool CanSendMessages()
    {
        return IsActive();
    }

    private static ConversationMember Create(
        Guid conversationId,
        Guid userId,
        ConversationMemberRole role,
        DateTimeOffset utcNow)
    {
        if (conversationId == Guid.Empty)
        {
            throw new DomainException("Conversation id is required.");
        }

        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        if (!Enum.IsDefined(role))
        {
            throw new DomainException("Conversation member role is invalid.");
        }

        return new ConversationMember
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            UserId = userId,
            Role = role,
            JoinedAt = utcNow,
            CreatedAt = utcNow
        };
    }

    private void EnsureActive()
    {
        if (!IsActive())
        {
            throw new DomainException("Inactive members cannot change roles.");
        }
    }

    private void Touch(DateTimeOffset utcNow)
    {
        UpdatedAt = utcNow;
    }
}
