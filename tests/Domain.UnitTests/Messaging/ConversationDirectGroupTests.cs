using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;
using Sportner.Domain.Messaging;

namespace Sportner.Domain.UnitTests.Messaging;

public sealed class ConversationDirectGroupTests
{
    [Fact]
    public void CreateDirectConversation_AddsBothMembers()
    {
        var creator = Guid.NewGuid();
        var other = Guid.NewGuid();
        var utcNow = DateTimeOffset.Parse("2026-08-11T12:00:00Z");

        var conversation = Conversation.CreateDirectConversation(creator, other, utcNow);

        Assert.Equal(ConversationType.Direct, conversation.Type);
        Assert.Null(conversation.EventId);
        Assert.Equal(2, conversation.ActiveMemberCount());
        Assert.True(conversation.IsOwner(creator));
        Assert.True(conversation.ContainsActiveMember(other));
    }

    [Fact]
    public void CreateDirectConversation_RejectsSelf()
    {
        var userId = Guid.NewGuid();
        Assert.Throws<DomainException>(() =>
            Conversation.CreateDirectConversation(userId, userId, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Direct_AddMember_Throws()
    {
        var conversation = Conversation.CreateDirectConversation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        Assert.Throws<DomainException>(() =>
            conversation.AddMember(Guid.NewGuid(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void CreateGroupConversation_EnforcesMaxMembers()
    {
        var owner = Guid.NewGuid();
        var tooMany = Enumerable.Range(0, Conversation.MaxGroupMembers)
            .Select(_ => Guid.NewGuid())
            .ToArray();

        Assert.Throws<DomainException>(() =>
            Conversation.CreateGroupConversation(
                owner,
                "Too big",
                tooMany,
                DateTimeOffset.UtcNow));
    }

    [Fact]
    public void InviteMember_RequiresOwnerOrModerator()
    {
        var owner = Guid.NewGuid();
        var member = Guid.NewGuid();
        var stranger = Guid.NewGuid();
        var utcNow = DateTimeOffset.UtcNow;

        var conversation = Conversation.CreateGroupConversation(
            owner,
            "Weekend run",
            [member],
            utcNow);

        Assert.Throws<DomainException>(() =>
            conversation.InviteMember(member, stranger, utcNow));

        conversation.InviteMember(owner, stranger, utcNow);
        Assert.True(conversation.ContainsActiveMember(stranger));
    }

    [Fact]
    public void Leave_AllowsNonOwner_InGroup()
    {
        var owner = Guid.NewGuid();
        var member = Guid.NewGuid();
        var utcNow = DateTimeOffset.UtcNow;

        var conversation = Conversation.CreateGroupConversation(
            owner,
            "Crew",
            [member],
            utcNow);

        conversation.Leave(member, utcNow);
        Assert.False(conversation.ContainsActiveMember(member));
    }
}
