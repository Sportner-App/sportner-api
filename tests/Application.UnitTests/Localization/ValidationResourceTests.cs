using System.Globalization;
using FluentAssertions;
using Sportner.Localization.Resources;

namespace Sportner.Application.UnitTests.Localization;

public class ValidationResourceTests
{
    [Theory]
    [InlineData("en-US", "No record was found matching the specified criteria.")]
    [InlineData("tr-TR", "Belirtilen kriterlere uygun kayıt bulunamadı.")]
    public void NotFoundResource_IsLocalized(string cultureName, string expected)
    {
        var value = ValidationResource.ResourceManager.GetString(
            "Exception_Base_NotFound_ByFilter",
            CultureInfo.GetCultureInfo(cultureName));

        value.Should().Be(expected);
    }
}
