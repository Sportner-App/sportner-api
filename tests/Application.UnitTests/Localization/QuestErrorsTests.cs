using System.Globalization;
using FluentAssertions;
using Sportner.Application.Features.Quests;
using Sportner.Localization.Resources;

namespace Sportner.Application.UnitTests.Localization;

public class QuestErrorsTests
{
    [Theory]
    [InlineData("en-US", "The request is not associated with an authenticated user.")]
    [InlineData("tr-TR", "Bu istek için giriş yapmış bir kullanıcı gerekiyor.")]
    public void NotAuthenticatedResource_IsLocalized(string cultureName, string expected)
    {
        var value = ErrorMessagesResource.ResourceManager.GetString(
            "Quest_NotAuthenticated",
            CultureInfo.GetCultureInfo(cultureName));

        value.Should().Be(expected);
    }

    [Fact]
    public void NotAuthenticated_ReflectsCurrentUICultureOnEachAccess()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
            QuestErrors.NotAuthenticated.Message.Should().Be("Bu istek için giriş yapmış bir kullanıcı gerekiyor.");

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            QuestErrors.NotAuthenticated.Message.Should().Be("The request is not associated with an authenticated user.");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public void NotAuthenticated_CodeStaysStableAcrossCultures()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
            var trCode = QuestErrors.NotAuthenticated.Code;

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var enCode = QuestErrors.NotAuthenticated.Code;

            trCode.Should().Be("Quest.NotAuthenticated");
            enCode.Should().Be("Quest.NotAuthenticated");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}
