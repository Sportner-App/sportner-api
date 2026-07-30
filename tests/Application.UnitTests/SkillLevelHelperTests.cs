using System.Text.Json;
using FluentAssertions;
using Sportner.Application.Helpers;

namespace Sportner.Application.UnitTests;

public class SkillLevelHelperTests
{
    [Fact]
    public void ResolveSkillLevel_ExactKey_ReturnsValue()
    {
        var json = """{"football":"advanced","tennis":"beginner"}""";
        SkillLevelHelper.ResolveSkillLevel(json, "football").Should().Be("advanced");
    }

    [Fact]
    public void ResolveSkillLevel_CaseInsensitive_ReturnsValue()
    {
        var json = """{"Football":"intermediate"}""";
        SkillLevelHelper.ResolveSkillLevel(json, "football").Should().Be("intermediate");
    }

    [Fact]
    public void ResolveSkillLevel_Missing_ReturnsNull()
    {
        SkillLevelHelper.ResolveSkillLevel("""{"tennis":"beginner"}""", "football").Should().BeNull();
    }

    [Fact]
    public void ToJsonbString_Object_ReturnsRawJson()
    {
        using var doc = JsonDocument.Parse("""{"football":"advanced"}""");
        var result = SkillLevelHelper.ToJsonbString(doc.RootElement);
        result.Should().Contain("football");
    }

    [Fact]
    public void ToUtc_Unspecified_TreatedAsUtc()
    {
        var input = new DateTime(1999, 3, 28, 0, 0, 0, DateTimeKind.Unspecified);
        var utc = SkillLevelHelper.ToUtc(input);
        utc.Kind.Should().Be(DateTimeKind.Utc);
        utc.Should().Be(new DateTime(1999, 3, 28, 0, 0, 0, DateTimeKind.Utc));
    }
}
