using FluentAssertions;
using Sportner.Domain.Enums;

namespace Sportner.Domain.UnitTests;

public class UserEventStatusTests
{
    [Theory]
    [InlineData(UserEventStatus.Pending, "pending")]
    [InlineData(UserEventStatus.Approved, "approved")]
    [InlineData(UserEventStatus.Rejected, "rejected")]
    public void ToDbValue_ReturnsExpected(UserEventStatus status, string expected)
    {
        status.ToDbValue().Should().Be(expected);
    }

    [Theory]
    [InlineData("pending", UserEventStatus.Pending)]
    [InlineData("APPROVED", UserEventStatus.Approved)]
    [InlineData(" Rejected ", UserEventStatus.Rejected)]
    public void ParseDbValue_ParsesExpected(string value, UserEventStatus expected)
    {
        UserEventStatusExtensions.ParseDbValue(value).Should().Be(expected);
    }

    [Fact]
    public void TryParseDbValue_Invalid_ReturnsFalse()
    {
        var ok = UserEventStatusExtensions.TryParseDbValue("unknown", out _);
        ok.Should().BeFalse();
    }
}
