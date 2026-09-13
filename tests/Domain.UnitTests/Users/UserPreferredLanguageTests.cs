using FluentAssertions;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Users;

namespace Sportner.Domain.UnitTests.Users;

public class UserPreferredLanguageTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RegisterWithPassword_DefaultsToTurkish_WhenNotSpecified()
    {
        var user = User.RegisterWithPassword("hash", "test@example.com", CreatedAt);

        user.PreferredLanguage.Should().Be(Language.Turkish);
    }

    [Fact]
    public void RegisterWithPassword_StoresRequestedLanguage()
    {
        var user = User.RegisterWithPassword(
            "hash", "test@example.com", CreatedAt, Language.English);

        user.PreferredLanguage.Should().Be(Language.English);
    }

    [Fact]
    public void RegisterWithExternalProvider_StoresRequestedLanguage()
    {
        var user = User.RegisterWithExternalProvider(
            ExternalLoginProvider.Google,
            "provider-id",
            "test@example.com",
            CreatedAt,
            Language.English);

        user.PreferredLanguage.Should().Be(Language.English);
    }

    [Fact]
    public void SetPreferredLanguage_UpdatesLanguage_AndTouchesUpdatedAt()
    {
        var user = User.RegisterWithPassword("hash", "test@example.com", CreatedAt);

        user.SetPreferredLanguage(Language.English, CreatedAt.AddMinutes(1));

        user.PreferredLanguage.Should().Be(Language.English);
        user.UpdatedAt.Should().Be(CreatedAt.AddMinutes(1));
    }

    [Fact]
    public void SetPreferredLanguage_IsNoOp_WhenUnchanged()
    {
        var user = User.RegisterWithPassword("hash", "test@example.com", CreatedAt);

        user.SetPreferredLanguage(Language.Turkish, CreatedAt.AddMinutes(1));

        user.UpdatedAt.Should().BeNull();
    }
}
