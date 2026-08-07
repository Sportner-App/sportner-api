using FluentAssertions;
using Sportner.Domain.Sports;

namespace Sportner.Domain.UnitTests.Sports;

public sealed class SportTests
{
    [Fact]
    public void Deactivate_SetsInactive_AndKeepsIdentity()
    {
        var now = DateTimeOffset.UtcNow;
        var sport = Sport.Create("Football", displayOrder: 1, now, slug: "football");

        sport.Deactivate(now.AddMinutes(1));

        sport.IsActive.Should().BeFalse();
        sport.CanBeUsed().Should().BeFalse();
        sport.Slug.Should().Be("football");
    }

    [Fact]
    public void Activate_RestoresActiveState()
    {
        var now = DateTimeOffset.UtcNow;
        var sport = Sport.Create("Football", displayOrder: 1, now, slug: "football", isActive: false);

        sport.Activate(now.AddMinutes(1));

        sport.IsActive.Should().BeTrue();
        sport.CanBeUsed().Should().BeTrue();
    }
}
