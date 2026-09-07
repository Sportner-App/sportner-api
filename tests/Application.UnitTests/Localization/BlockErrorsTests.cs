using System.Globalization;
using FluentAssertions;
using Sportner.Application.Features.Social;
using Sportner.Localization.Resources;

namespace Sportner.Application.UnitTests.Localization;

public class BlockErrorsTests
{
    [Theory]
    [InlineData("en-US", "Users cannot block themselves.")]
    [InlineData("tr-TR", "Kendini engelleyemezsin.")]
    public void SelfBlockResource_IsLocalized(string cultureName, string expected)
    {
        var value = ErrorMessagesResource.ResourceManager.GetString(
            "Block_SelfBlock",
            CultureInfo.GetCultureInfo(cultureName));

        value.Should().Be(expected);
    }

    [Fact]
    public void SelfBlock_ReflectsCurrentUICultureOnEachAccess()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
            BlockErrors.SelfBlock.Message.Should().Be("Kendini engelleyemezsin.");

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            BlockErrors.SelfBlock.Message.Should().Be("Users cannot block themselves.");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public void SelfBlock_CodeStaysStableAcrossCultures()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
            var trCode = BlockErrors.SelfBlock.Code;

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var enCode = BlockErrors.SelfBlock.Code;

            trCode.Should().Be("Block.SelfBlock");
            enCode.Should().Be("Block.SelfBlock");
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}
