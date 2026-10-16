using FluentAssertions;
using Sportner.Domain.Common.Exceptions;
using Sportner.Domain.Messaging;

namespace Sportner.Domain.UnitTests.Messaging;

public class MessageMentionTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);

    private static Message TextMessage(Guid senderId) =>
        Message.CreateText(Guid.NewGuid(), senderId, "@ahmet geliyor musun", Now);

    [Fact]
    public void SetMentions_StoresEachPerson()
    {
        var message = TextMessage(Guid.NewGuid());
        var ahmet = Guid.NewGuid();

        message.SetMentions([(ahmet, "ahmet")], Now);

        message.Mentions.Should().ContainSingle()
            .Which.UserId.Should().Be(ahmet);
    }

    [Fact]
    public void SetMentions_WithTheSender_Throws()
    {
        var sender = Guid.NewGuid();
        var message = TextMessage(sender);

        var action = () => message.SetMentions([(sender, "ben")], Now);

        action.Should().Throw<DomainException>();
    }

    [Fact]
    public void SetMentions_IgnoresDuplicates()
    {
        var message = TextMessage(Guid.NewGuid());
        var ahmet = Guid.NewGuid();

        message.SetMentions([(ahmet, "ahmet"), (ahmet, "ahmet")], Now);

        message.Mentions.Should().ContainSingle();
    }

    [Fact]
    public void SetMentions_BeyondTheLimit_Throws()
    {
        var message = TextMessage(Guid.NewGuid());
        var tooMany = Enumerable.Range(0, 21)
            .Select(index => (Guid.NewGuid(), $"kisi{index}"))
            .ToList();

        var action = () => message.SetMentions(tooMany, Now);

        action.Should().Throw<DomainException>();
    }
}
