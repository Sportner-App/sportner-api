using FluentAssertions;
using Sportner.Domain.Enums;

namespace Sportner.Domain.UnitTests;

public class ParticipantStatusTests
{
    [Theory]
    [InlineData(ParticipantStatus.Pending, "pending")]
    [InlineData(ParticipantStatus.Approved, "approved")]
    [InlineData(ParticipantStatus.Rejected, "rejected")]
    public void ToDbValue_ReturnsExpected(ParticipantStatus status, string expected)
    {
        status.ToDbValue().Should().Be(expected);
    }

    [Theory]
    [InlineData("pending", ParticipantStatus.Pending)]
    [InlineData("APPROVED", ParticipantStatus.Approved)]
    [InlineData(" Rejected ", ParticipantStatus.Rejected)]
    public void ParseDbValue_ParsesExpected(string value, ParticipantStatus expected)
    {
        ParticipantStatusExtensions.ParseDbValue(value).Should().Be(expected);
    }

    [Fact]
    public void TryParseDbValue_Invalid_ReturnsFalse()
    {
        var ok = ParticipantStatusExtensions.TryParseDbValue("unknown", out _);
        ok.Should().BeFalse();
    }
}
