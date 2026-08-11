using FluentAssertions;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;
using Sportner.Domain.Users;

namespace Sportner.Domain.UnitTests.Users;

public class UserOnboardingTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_LeavesOnboardingIncomplete()
    {
        var user = CreateActiveUser();

        user.HasCompletedOnboarding().Should().BeFalse();
        user.OnboardingCompletedAt.Should().BeNull();
    }

    [Fact]
    public void CompleteOnboarding_WithoutProfile_Throws()
    {
        var user = CreateActiveUser();
        user.AddSport(Guid.NewGuid(), SkillLevel.Beginner, CreatedAt);

        var action = () => user.CompleteOnboarding(CreatedAt);

        action.Should().Throw<DomainException>();
    }

    [Fact]
    public void CompleteOnboarding_WithoutSport_Throws()
    {
        var user = CreateActiveUser();
        AttachProfile(user);

        var action = () => user.CompleteOnboarding(CreatedAt);

        action.Should().Throw<DomainException>();
    }

    [Fact]
    public void CompleteOnboarding_WithProfileAndSport_StampsCompletionDate()
    {
        var user = CreateActiveUser();
        AttachProfile(user);
        user.AddSport(Guid.NewGuid(), SkillLevel.Beginner, CreatedAt);
        var completedAt = CreatedAt.AddMinutes(5);

        user.CompleteOnboarding(completedAt);

        user.HasCompletedOnboarding().Should().BeTrue();
        user.OnboardingCompletedAt.Should().Be(completedAt);
    }

    [Fact]
    public void CompleteOnboarding_WhenAlreadyCompleted_KeepsFirstCompletionDate()
    {
        var user = CreateActiveUser();
        AttachProfile(user);
        user.AddSport(Guid.NewGuid(), SkillLevel.Beginner, CreatedAt);
        var completedAt = CreatedAt.AddMinutes(5);
        user.CompleteOnboarding(completedAt);

        user.CompleteOnboarding(completedAt.AddDays(1));

        user.OnboardingCompletedAt.Should().Be(completedAt);
    }

    private static User CreateActiveUser()
    {
        var user = User.RegisterWithPassword("test-hash", CreatedAt);
        return user;
    }

    private static void AttachProfile(User user)
    {
        user.AttachUserProfile(UserProfile.Create(user.Id, "username", "First", CreatedAt));
    }
}
