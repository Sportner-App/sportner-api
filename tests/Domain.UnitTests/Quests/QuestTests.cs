using FluentAssertions;
using Sportner.Domain.Quests;

namespace Sportner.Domain.UnitTests.Quests;

public sealed class QuestTests
{
    private static Quest CreateQuest(DateTimeOffset now, string? titleEn = null, string? descriptionEn = null) =>
        Quest.Create(
            "ATTEND_3",
            "3 etkinliğe katıl",
            "Üç etkinlikte katılımını onaylat.",
            "events_attended",
            3,
            Guid.NewGuid(),
            1,
            now,
            titleEn,
            descriptionEn);

    [Fact]
    public void Create_TrimsEnglishText_WhenProvided()
    {
        var now = DateTimeOffset.UtcNow;

        var quest = CreateQuest(
            now,
            "  Attend 3 events  ",
            "  Get your attendance confirmed at three events.  ");

        quest.TitleEn.Should().Be("Attend 3 events");
        quest.DescriptionEn.Should().Be("Get your attendance confirmed at three events.");
    }

    [Fact]
    public void Create_LeavesEnglishTextNull_WhenNotProvided()
    {
        var now = DateTimeOffset.UtcNow;

        var quest = CreateQuest(now);

        quest.TitleEn.Should().BeNull();
        quest.DescriptionEn.Should().BeNull();
    }

    [Fact]
    public void RenameEnglish_UpdatesTitle_AndTouchesUpdatedAt()
    {
        var now = DateTimeOffset.UtcNow;
        var quest = CreateQuest(now);

        quest.RenameEnglish("Attend 3 events", now.AddMinutes(1));

        quest.TitleEn.Should().Be("Attend 3 events");
        quest.UpdatedAt.Should().Be(now.AddMinutes(1));
    }

    [Fact]
    public void UpdateDescriptionEnglish_ClearsDescription_WhenNullPassed()
    {
        var now = DateTimeOffset.UtcNow;
        var quest = CreateQuest(now, "Attend 3 events", "Get your attendance confirmed at three events.");

        quest.UpdateDescriptionEnglish(null, now.AddMinutes(1));

        quest.DescriptionEn.Should().BeNull();
    }
}
