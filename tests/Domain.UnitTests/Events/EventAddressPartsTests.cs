using FluentAssertions;
using Sportner.Domain.Events;

namespace Sportner.Domain.UnitTests.Events;

public sealed class EventAddressPartsTests
{
    [Theory]
    [InlineData("İdealtepe, Hatboyu Yolu Cd. No:43 D:29, 34841 Maltepe/İstanbul, Türkiye", "Maltepe", "İstanbul")]
    [InlineData("Caferağa, Moda Cd. No:192/1, 34710 Moda/İstanbul, Türkiye", "Moda", "İstanbul")]
    [InlineData("Bakırköy/İstanbul, Türkiye", "Bakırköy", "İstanbul")]
    [InlineData("Some St, 48300 Fethiye/Muğla, Turkey", "Fethiye", "Muğla")]
    public void Parses_DistrictAndCity(string address, string district, string city)
    {
        EventAddressParts.Parse(address).Should().Be((district, city));
    }

    [Theory]
    [InlineData("Istanbul, İstanbul, Türkiye")]
    [InlineData("")]
    [InlineData(null)]
    public void UnknownShape_ReturnsNulls(string? address)
    {
        EventAddressParts.Parse(address).Should().Be(((string?)null, (string?)null));
    }
}
