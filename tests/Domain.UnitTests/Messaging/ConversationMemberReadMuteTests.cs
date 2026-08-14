using Sportner.Domain.Common.Exceptions;
using Sportner.Domain.Messaging;

namespace Sportner.Domain.UnitTests.Messaging;

public sealed class ConversationMemberReadMuteTests
{
    [Fact]
    public void MarkRead_AdvancesForwardOnly()
    {
        var now = DateTimeOffset.Parse("2026-08-14T10:00:00Z");
        var member = ConversationMember.CreateMember(Guid.NewGuid(), Guid.NewGuid(), now);
        var firstMessageId = Guid.NewGuid();
        var secondMessageId = Guid.NewGuid();
        var firstAt = now.AddMinutes(1);
        var secondAt = now.AddMinutes(2);

        member.MarkRead(firstMessageId, firstAt, now.AddMinutes(3));
        Assert.Equal(firstMessageId, member.LastReadMessageId);
        Assert.Equal(firstAt, member.LastReadAt);

        member.MarkRead(secondMessageId, secondAt, now.AddMinutes(4));
        Assert.Equal(secondMessageId, member.LastReadMessageId);
        Assert.Equal(secondAt, member.LastReadAt);

        // Older cursor must not move backwards.
        member.MarkRead(firstMessageId, firstAt, now.AddMinutes(5));
        Assert.Equal(secondMessageId, member.LastReadMessageId);
        Assert.Equal(secondAt, member.LastReadAt);
    }

    [Fact]
    public void Mute_And_Unmute_Work()
    {
        var now = DateTimeOffset.Parse("2026-08-14T10:00:00Z");
        var member = ConversationMember.CreateMember(Guid.NewGuid(), Guid.NewGuid(), now);

        Assert.False(member.IsMuted(now));

        member.Mute(now.AddHours(2), now);
        Assert.True(member.IsMuted(now.AddHours(1)));
        Assert.False(member.IsMuted(now.AddHours(3)));

        member.Unmute(now.AddMinutes(1));
        Assert.Null(member.MutedUntil);
        Assert.False(member.IsMuted(now.AddMinutes(2)));
    }

    [Fact]
    public void Mute_RejectsPastExpiry()
    {
        var now = DateTimeOffset.UtcNow;
        var member = ConversationMember.CreateMember(Guid.NewGuid(), Guid.NewGuid(), now);

        Assert.Throws<DomainException>(() => member.Mute(now.AddSeconds(-1), now));
    }
}
