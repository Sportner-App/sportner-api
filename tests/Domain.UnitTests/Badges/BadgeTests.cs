using FluentAssertions;
using Sportner.Domain.Badges;
using Sportner.Domain.Common.Enums;

namespace Sportner.Domain.UnitTests.Badges;

public sealed class BadgeTests
{
    private static Badge CreateBadge(DateTimeOffset now, string? nameEn = null, string? descriptionEn = null) =>
        Badge.Create(
            "FIRST_EVENT",
            "İlk Etkinlik",
            "İlk etkinliğine katıldın.",
            "badges/first-event.png",
            BadgeCategory.Events,
            BadgeRarity.Common,
            50,
            1,
            now,
            nameEn,
            descriptionEn);

    [Fact]
    public void Create_TrimsEnglishText_WhenProvided()
    {
        var now = DateTimeOffset.UtcNow;

        var badge = CreateBadge(now, "  First Event  ", "  You attended your first event.  ");

        badge.NameEn.Should().Be("First Event");
        badge.DescriptionEn.Should().Be("You attended your first event.");
    }

    [Fact]
    public void Create_LeavesEnglishTextNull_WhenNotProvided()
    {
        var now = DateTimeOffset.UtcNow;

        var badge = CreateBadge(now);

        badge.NameEn.Should().BeNull();
        badge.DescriptionEn.Should().BeNull();
    }

    [Fact]
    public void RenameEnglish_UpdatesName_AndTouchesUpdatedAt()
    {
        var now = DateTimeOffset.UtcNow;
        var badge = CreateBadge(now);

        badge.RenameEnglish("First Event", now.AddMinutes(1));

        badge.NameEn.Should().Be("First Event");
        badge.UpdatedAt.Should().Be(now.AddMinutes(1));
    }

    [Fact]
    public void UpdateDescriptionEnglish_ClearsDescription_WhenNullPassed()
    {
        var now = DateTimeOffset.UtcNow;
        var badge = CreateBadge(now, "First Event", "You attended your first event.");

        badge.UpdateDescriptionEnglish(null, now.AddMinutes(1));

        badge.DescriptionEn.Should().BeNull();
    }
}
