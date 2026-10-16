using FluentAssertions;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Users;

namespace Sportner.Domain.UnitTests.Users;

/// <summary>
/// Self-service deletion is a soft delete, but it has to release the identifiers
/// the account occupies — otherwise the person can never come back.
/// </summary>
public class UserDeletionTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Delete_MarksTheAccountDeleted()
    {
        var user = SocialUser();

        user.Delete(Now);

        user.Status.Should().Be(UserStatus.Deleted);
    }

    /// <summary>
    /// The email sits behind a partial unique index, so leaving it in place made
    /// registering again fail with EmailTaken.
    /// </summary>
    [Fact]
    public void Delete_ReleasesTheEmail()
    {
        var user = SocialUser();
        user.Email.Should().NotBeNull();

        user.Delete(Now);

        user.Email.Should().BeNull();
        user.EmailVerifiedAt.Should().BeNull();
    }

    /// <summary>
    /// (Provider, ProviderUserId) is unique, so a link left pointing at the deleted
    /// row meant signing in with the same Google or Apple account was rejected with
    /// AccountNotAccessible, permanently.
    /// </summary>
    [Fact]
    public void Delete_ReleasesExternalLogins()
    {
        var user = SocialUser();
        user.ExternalLogins.Should().HaveCount(1);

        user.Delete(Now);

        user.ExternalLogins.Should().BeEmpty();
    }

    [Fact]
    public void Delete_ClearsThePasswordHash()
    {
        var user = User.RegisterWithPassword("hash", "someone@example.com", Now);

        user.Delete(Now);

        user.PasswordHash.Should().BeNull();
    }

    [Fact]
    public void Delete_RevokesActiveSessions()
    {
        var user = SocialUser();
        user.CreateSession("refresh-hash", Now.AddDays(7), Now);

        user.Delete(Now);

        user.Sessions.Should().HaveCount(1);
        user.Sessions.Should().OnlyContain(session => session.RevokedAt != null);
    }

    [Fact]
    public void Delete_WhenAlreadyDeleted_Throws()
    {
        var user = SocialUser();
        user.Delete(Now);

        var action = () => user.Delete(Now);

        action.Should().Throw<Sportner.Domain.Common.Exceptions.DomainException>();
    }

    private static User SocialUser() =>
        User.RegisterWithExternalProvider(
            ExternalLoginProvider.Google,
            "google-subject-1",
            "social@example.com",
            Now);
}
