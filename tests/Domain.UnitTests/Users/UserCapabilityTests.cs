using FluentAssertions;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Users;

namespace Sportner.Domain.UnitTests.Users;

/// <summary>
/// <see cref="User.CanAuthenticate"/> and <see cref="User.CanCreateContent"/> answer
/// two different questions, and the difference matters: authentication needs a
/// credential to exist, content creation does not.
/// </summary>
public class UserCapabilityTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CanCreateContent_ForSocialSignUp_IsTrue()
    {
        var user = SocialUser();

        user.CanCreateContent().Should().BeTrue();
    }

    /// <summary>
    /// The regression this file exists for. A social sign-up has no password, so its
    /// only credential is the ExternalLogins collection — a backing field that EF
    /// leaves empty unless the query included it, which no content handler does.
    /// CanCreateContent therefore must not consult credentials at all: anyone
    /// reaching a content handler already passed JWT authentication.
    ///
    /// An active user holding no credential whatsoever stands in for that unloaded
    /// collection. Before the fix this returned false and every Google/Apple user
    /// was blocked from creating events, posts, comments and organizations.
    /// </summary>
    [Fact]
    public void CanCreateContent_WithoutAnyCredential_IsTrue()
    {
        var user = ActiveUserWithoutCredential();

        user.CanCreateContent().Should().BeTrue();
    }

    [Fact]
    public void CanAuthenticate_WithoutAnyCredential_IsFalse()
    {
        var user = ActiveUserWithoutCredential();

        user.CanAuthenticate().Should().BeFalse();
    }

    [Fact]
    public void CanAuthenticate_ForSocialSignUp_IsTrue()
    {
        SocialUser().CanAuthenticate().Should().BeTrue();
    }

    [Fact]
    public void CanAuthenticate_ForPasswordSignUp_IsTrue()
    {
        PasswordUser().CanAuthenticate().Should().BeTrue();
    }

    [Fact]
    public void CanCreateContent_WhilePendingVerification_IsFalse()
    {
        var user = User.Create("+905551112233", Now);

        user.Status.Should().Be(UserStatus.PendingVerification);
        user.CanCreateContent().Should().BeFalse();
    }

    [Fact]
    public void CanCreateContent_WhenSuspended_IsFalse()
    {
        var user = SocialUser();
        user.Suspend(Now);

        user.CanCreateContent().Should().BeFalse();
    }

    [Fact]
    public void CanCreateContent_WhenBanned_IsFalse()
    {
        var user = SocialUser();
        user.Ban(Now);

        user.CanCreateContent().Should().BeFalse();
    }

    [Fact]
    public void CanCreateContent_AfterDeletion_IsFalse()
    {
        var user = SocialUser();
        user.Delete(Now);

        user.CanCreateContent().Should().BeFalse();
    }

    private static User PasswordUser() =>
        User.RegisterWithPassword("hash", "password@example.com", Now);

    private static User SocialUser() =>
        User.RegisterWithExternalProvider(
            ExternalLoginProvider.Google,
            "google-subject-1",
            "social@example.com",
            Now);

    /// <summary>Active, but with neither a password nor an external login.</summary>
    private static User ActiveUserWithoutCredential()
    {
        var user = User.Create("+905551112233", Now);
        user.VerifyPhoneNumber(Now);
        user.Activate(Now);

        return user;
    }
}
