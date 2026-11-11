using FluentAssertions;
using Sportner.Application.Features.Identity.ContentPolicy;

namespace Sportner.Application.UnitTests.Features.Identity.ContentPolicy;

public sealed class IdentityContentPolicyTests
{
    [Theory]
    [InlineData("porno")]
    [InlineData("P0rn0")]
    [InlineData("pooorno_tr")]
    [InlineData("xxx.ali")]
    [InlineData("sik_adam")]
    [InlineData("orospu123")]
    [InlineData("admin")]
    [InlineData("sportner.destek")]
    [InlineData("kaltak_34")]
    [InlineData("taşaklar")]
    [InlineData("big_tits")]
    [InlineData("bigtits")]
    [InlineData("motherfucker")]
    public void IsUsernameAllowed_RejectsOffensiveOrReserved(string username) =>
        IdentityContentPolicy.IsUsernameAllowed(username).Should().BeFalse();

    [Theory]
    [InlineData("yagizerdenler")]
    [InlineData("cerenkarakus")]
    [InlineData("nazim.hikmet")]
    [InlineData("fizikci_ali")]
    [InlineData("sikke.koleksiyon")]
    [InlineData("amasya_kosu")]
    [InlineData("gotham")]
    [InlineData("sexton")]
    [InlineData("analiz.tr")]
    [InlineData("big.black.bull")]
    [InlineData("butterfly")]
    [InlineData("assist_king")]
    [InlineData("x.ahmet")]
    public void IsUsernameAllowed_AcceptsInnocentNames(string username) =>
        IdentityContentPolicy.IsUsernameAllowed(username).Should().BeTrue();

    [Theory]
    [InlineData("Porno", false)]
    [InlineData("Sik", false)]
    [InlineData("Nazım", true)]
    [InlineData("Şükrü", true)]
    [InlineData("Amine", true)]
    [InlineData(null, true)]
    public void IsNameAllowed_ChecksWholeWordsForShortTerms(string? name, bool expected) =>
        IdentityContentPolicy.IsNameAllowed(name).Should().Be(expected);

    [Theory]
    [InlineData("xecin43048@jysnk.com", false)]
    [InlineData("a@inbox.mailinator.com", false)]
    [InlineData("YOPMAIL@YOPMAIL.COM", false)]
    [InlineData("yagiz@gmail.com", true)]
    [InlineData("someone@company.com.tr", true)]
    public void IsEmailAllowed_RejectsDisposableDomains(string email, bool expected) =>
        IdentityContentPolicy.IsEmailAllowed(email).Should().Be(expected);
}
