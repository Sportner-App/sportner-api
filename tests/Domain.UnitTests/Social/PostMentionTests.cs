using FluentAssertions;
using Sportner.Domain.Common.Exceptions;
using Sportner.Domain.Social;

namespace Sportner.Domain.UnitTests.Social;

public class PostMentionTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void SetMentions_StoresEachPerson()
    {
        var post = Post.Create(Guid.NewGuid(), "Maça geliyor musunuz?", Now);
        var ahmet = Guid.NewGuid();
        var mehmet = Guid.NewGuid();

        post.SetMentions([(ahmet, "ahmet"), (mehmet, "mehmet")], Now);

        post.Mentions.Should().HaveCount(2);
        post.Mentions.Select(mention => mention.UserId)
            .Should().BeEquivalentTo([ahmet, mehmet]);
    }

    /// <summary>
    /// Düzenlemede de kullanıldığı için ekleme değil değiştirme: kullanıcı
    /// metinden bir etiketi sildiğinde kaydı da gitmeli, yoksa kişi gönderide
    /// adı geçmediği hâlde etiketli kalır.
    /// </summary>
    [Fact]
    public void SetMentions_ReplacesThePreviousList()
    {
        var post = Post.Create(Guid.NewGuid(), "metin", Now);
        var ahmet = Guid.NewGuid();
        var mehmet = Guid.NewGuid();

        post.SetMentions([(ahmet, "ahmet")], Now);
        post.SetMentions([(mehmet, "mehmet")], Now);

        post.Mentions.Should().ContainSingle()
            .Which.UserId.Should().Be(mehmet);
    }

    [Fact]
    public void SetMentions_IgnoresDuplicates()
    {
        var post = Post.Create(Guid.NewGuid(), "metin", Now);
        var ahmet = Guid.NewGuid();

        post.SetMentions([(ahmet, "ahmet"), (ahmet, "ahmet")], Now);

        post.Mentions.Should().ContainSingle();
    }

    [Fact]
    public void SetMentions_WithTheAuthor_Throws()
    {
        var author = Guid.NewGuid();
        var post = Post.Create(author, "metin", Now);

        var action = () => post.SetMentions([(author, "ben")], Now);

        action.Should().Throw<DomainException>();
    }

    [Fact]
    public void SetMentions_BeyondTheLimit_Throws()
    {
        var post = Post.Create(Guid.NewGuid(), "metin", Now);
        var tooMany = Enumerable.Range(0, 21)
            .Select(index => (Guid.NewGuid(), $"kisi{index}"))
            .ToList();

        var action = () => post.SetMentions(tooMany, Now);

        action.Should().Throw<DomainException>();
    }

    /// <summary>
    /// Kullanıcı adı yazıldığı andaki hâliyle saklanıyor: içerikteki "@ahmet"
    /// yazısını etikete bağlamak için gerekiyor. Kişi adını değiştirse de
    /// bağlantı kimliğe gittiği için doğru kalır.
    /// </summary>
    [Fact]
    public void SetMentions_KeepsTheUsernameAsWritten()
    {
        var post = Post.Create(Guid.NewGuid(), "metin", Now);
        var ahmet = Guid.NewGuid();

        post.SetMentions([(ahmet, "  ahmet  ")], Now);

        post.Mentions.Single().Username.Should().Be("ahmet");
    }
}
