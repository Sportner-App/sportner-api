using FluentAssertions;
using Sportner.Domain.Common.Exceptions;
using Sportner.Domain.Users;

namespace Sportner.Domain.UnitTests.Users;

public class UserProfileTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_PersistsInitialUsernameChangeTimestamp()
    {
        var profile = UserProfile.Create(
            Guid.NewGuid(),
            "first_username",
            "First",
            CreatedAt);

        profile.UsernameChangedAt.Should().Be(CreatedAt);
    }

    [Fact]
    public void UpdateUsername_BeforeThirtyDays_Throws()
    {
        var profile = UserProfile.Create(
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
        var profile = UserProfile.Create(
            Guid.NewGuid(),
            "first_username",
            "First",
            CreatedAt);
        var changedAt = CreatedAt.AddDays(30);

        profile.UpdateUsername("second_username", changedAt);

        profile.Username.Should().Be("second_username");
        profile.UsernameChangedAt.Should().Be(changedAt);
    }

    [Fact]
    public void UpdatePersonalDetails_FirstSet_AcceptsBirthDate()
    {
        var profile = UserProfile.Create(
            Guid.NewGuid(),
            "first_username",
            "First",
            CreatedAt);

        profile.UpdatePersonalDetails(1, new DateOnly(1995, 1, 1), CreatedAt);

        profile.BirthDate.Should().Be(new DateOnly(1995, 1, 1));
    }

    [Fact]
    public void UpdatePersonalDetails_ResendingTheSameBirthDate_DoesNotThrow()
    {
        var profile = UserProfile.Create(
            Guid.NewGuid(),
            "first_username",
            "First",
            CreatedAt);
        profile.UpdatePersonalDetails(1, new DateOnly(1995, 1, 1), CreatedAt);

        var action = () => profile.UpdatePersonalDetails(2, new DateOnly(1995, 1, 1), CreatedAt);

        action.Should().NotThrow();
        profile.Gender.Should().Be(2);
    }

    [Fact]
    public void UpdatePersonalDetails_ChangingAnAlreadySetBirthDate_Throws()
    {
        var profile = UserProfile.Create(
            Guid.NewGuid(),
            "first_username",
            "First",
            CreatedAt);
        profile.UpdatePersonalDetails(1, new DateOnly(1995, 1, 1), CreatedAt);

        var action = () => profile.UpdatePersonalDetails(1, new DateOnly(2008, 1, 1), CreatedAt);

        action.Should().Throw<DomainException>();
        profile.BirthDate.Should().Be(new DateOnly(1995, 1, 1));
    }
}
