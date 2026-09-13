using FluentAssertions;
using Sportner.Domain.Sports;

namespace Sportner.Domain.UnitTests.Sports;

public sealed class SportCategoryTests
{
    [Fact]
    public void Create_TrimsEnglishName_WhenProvided()
    {
        var now = DateTimeOffset.UtcNow;

        var category = SportCategory.Create(
            "Takım Sporları", "takim-sporlari", 1, now, nameEn: "  Team Sports  ");

        category.NameEn.Should().Be("Team Sports");
    }

    [Fact]
    public void Create_LeavesEnglishNameNull_WhenNotProvided()
    {
        var now = DateTimeOffset.UtcNow;

        var category = SportCategory.Create("Takım Sporları", "takim-sporlari", 1, now);

        category.NameEn.Should().BeNull();
    }

    [Fact]
    public void RenameEnglish_UpdatesName_AndTouchesUpdatedAt()
    {
        var now = DateTimeOffset.UtcNow;
        var category = SportCategory.Create("Takım Sporları", "takim-sporlari", 1, now);

        category.RenameEnglish("Team Sports", now.AddMinutes(1));

        category.NameEn.Should().Be("Team Sports");
        category.UpdatedAt.Should().Be(now.AddMinutes(1));
    }

    [Fact]
    public void RenameEnglish_ClearsName_WhenNullPassed()
    {
        var now = DateTimeOffset.UtcNow;
        var category = SportCategory.Create(
            "Takım Sporları", "takim-sporlari", 1, now, nameEn: "Team Sports");

        category.RenameEnglish(null, now.AddMinutes(1));

        category.NameEn.Should().BeNull();
    }
}
