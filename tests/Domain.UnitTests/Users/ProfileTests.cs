using FluentAssertions;
using Sportner.Domain.Common.Exceptions;
using Sportner.Domain.Users;

namespace Sportner.Domain.UnitTests.Users;

public class ProfileTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_PersistsInitialUsernameChangeTimestamp()
    {
        var profile = Profile.Create(
            Guid.NewGuid(),
            "first_username",
            "First",
            CreatedAt);

        profile.UsernameChangedAt.Should().Be(CreatedAt);
    }

    [Fact]
    public void UpdateUsername_BeforeThirtyDays_Throws()
    {
        var profile = Profile.Create(
            Guid.NewGuid(),
            "first_username",
            "First",
            CreatedAt);

        var action = () => profile.UpdateUsername(
            "second_username",
            CreatedAt.AddDays(29));

        action.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdateUsername_AfterThirtyDays_UpdatesTimestamp()
    {
        var profile = Profile.Create(
            Guid.NewGuid(),
            "first_username",
            "First",
            CreatedAt);
        var changedAt = CreatedAt.AddDays(30);

        profile.UpdateUsername("second_username", changedAt);

        profile.Username.Should().Be("second_username");
        profile.UsernameChangedAt.Should().Be(changedAt);
    }
}
