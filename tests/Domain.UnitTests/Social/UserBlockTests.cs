using FluentAssertions;
using Sportner.Domain.Common.Exceptions;
using Sportner.Domain.Social;

namespace Sportner.Domain.UnitTests.Social;

public sealed class UserBlockTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 31, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_PersistsPair()
    {
        var blocker = Guid.NewGuid();
        var blocked = Guid.NewGuid();

        var block = UserBlock.Create(blocker, blocked, Now);

        block.BlockerUserId.Should().Be(blocker);
        block.BlockedUserId.Should().Be(blocked);
        block.CreatedAt.Should().Be(Now);
        block.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_WhenSelf_Throws()
    {
        var userId = Guid.NewGuid();

        var action = () => UserBlock.Create(userId, userId, Now);

        action.Should().Throw<DomainException>()
            .WithMessage("Users cannot block themselves.");
    }

    [Fact]
    public void Create_WhenEmptyId_Throws()
    {
        var action = () => UserBlock.Create(Guid.Empty, Guid.NewGuid(), Now);

        action.Should().Throw<DomainException>();
    }
}
