using FluentAssertions;
using Sportner.Application.Helpers;

namespace Sportner.Application.UnitTests;

public class GeoHelperTests
{
    [Fact]
    public void HaversineKm_SamePoint_IsZero()
    {
        GeoHelper.HaversineKm(41.0, 29.0, 41.0, 29.0).Should().BeApproximately(0, 0.001);
    }

    [Fact]
    public void HaversineKm_KnownDistance_IsApproximate()
    {
        // Istanbul ~ Ankara roughly 350 km
        var km = GeoHelper.HaversineKm(41.0082, 28.9784, 39.9334, 32.8597);
        km.Should().BeInRange(300, 400);
    }

    [Fact]
    public void DegreesToRadians_180_IsPi()
    {
        GeoHelper.DegreesToRadians(180).Should().BeApproximately(Math.PI, 0.000001);
    }
}
