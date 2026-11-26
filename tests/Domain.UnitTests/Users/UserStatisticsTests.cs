using FluentAssertions;
using Sportner.Domain.Common.Exceptions;
using Sportner.Domain.Users;

namespace Sportner.Domain.UnitTests.Users;

public class UserStatisticsTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void UpdateReviewSummary_SetsAverageAndTotal()
    {
        var statistics = UserStatistics.Create(Guid.NewGuid(), Now);

        statistics.UpdateReviewSummary(4.5m, 3, Now);

        statistics.AverageRating.Should().Be(4.5m);
        statistics.TotalReviews.Should().Be(3);
    }

    [Fact]
    public void UpdateReviewSummary_NegativeTotal_Throws()
    {
        var statistics = UserStatistics.Create(Guid.NewGuid(), Now);

        var act = () => statistics.UpdateReviewSummary(3m, -1, Now);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(5.1)]
    public void UpdateReviewSummary_AverageOutOfRange_Throws(double average)
    {
        var statistics = UserStatistics.Create(Guid.NewGuid(), Now);

        var act = () => statistics.UpdateReviewSummary((decimal)average, 1, Now);

        act.Should().Throw<DomainException>();
    }
}
