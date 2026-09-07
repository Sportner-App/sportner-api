using System.Globalization;
using FluentAssertions;
using Sportner.Application.Features.Social;
using Sportner.Localization.Resources;

namespace Sportner.Application.UnitTests.Localization;

public class SocialErrorsTests
{
    [Theory]
    [InlineData("en-US", "The friendship was not found.")]
    [InlineData("tr-TR", "Arkadaşlık bulunamadı.")]
    public void NotFoundResource_IsLocalized(string cultureName, string expected)
    {
        var value = ErrorMessagesResource.ResourceManager.GetString(
            "Friendship_NotFound",
            CultureInfo.GetCultureInfo(cultureName));

        value.Should().Be(expected);
    }

    [Fact]
    public void NotFound_ReflectsCurrentUICultureOnEachAccess()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
            FriendshipErrors.NotFound.Message.Should().Be("Arkadaşlık bulunamadı.");

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            FriendshipErrors.NotFound.Message.Should().Be("The friendship was not found.");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public void NotFound_CodeStaysStableAcrossCultures()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
            var trCode = FriendshipErrors.NotFound.Code;

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var enCode = FriendshipErrors.NotFound.Code;

            trCode.Should().Be("Friendship.NotFound");
            enCode.Should().Be("Friendship.NotFound");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}


public class PostErrorsTests
{
    [Theory]
    [InlineData("en-US", "The post was not found.")]
    [InlineData("tr-TR", "Gönderi bulunamadı.")]
    public void NotFoundResource_IsLocalized(string cultureName, string expected)
    {
        var value = ErrorMessagesResource.ResourceManager.GetString(
            "Post_NotFound",
            CultureInfo.GetCultureInfo(cultureName));

        value.Should().Be(expected);
    }

    [Fact]
    public void NotFound_ReflectsCurrentUICultureOnEachAccess()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
            PostErrors.NotFound.Message.Should().Be("Gönderi bulunamadı.");

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            PostErrors.NotFound.Message.Should().Be("The post was not found.");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public void NotFound_CodeStaysStableAcrossCultures()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
            var trCode = PostErrors.NotFound.Code;

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var enCode = PostErrors.NotFound.Code;

            trCode.Should().Be("Post.NotFound");
            enCode.Should().Be("Post.NotFound");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}
