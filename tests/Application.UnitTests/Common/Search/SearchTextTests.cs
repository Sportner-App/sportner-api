using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Common.Search;
using Sportner.Infrastructure.Persistence;

namespace Sportner.Application.UnitTests.Common.Search;

public class SearchTextTests
{
    /// <summary>
    /// Klavye cümle başında ilk harfi büyütüyor, kullanıcı da Türkçe klavye
    /// kullanmak zorunda değil. Dört "i" biçiminin ve aksanlı harflerin hepsi
    /// aynı metne inmeli, yoksa arama sessizce boş dönüyor.
    /// </summary>
    [Theory]
    [InlineData("İstanbul", "istanbul")]
    [InlineData("Istanbul", "istanbul")]
    [InlineData("ISTANBUL", "istanbul")]
    [InlineData("ıstanbul", "istanbul")]
    [InlineData("Iğdır", "igdir")]
    [InlineData("Yüzme", "yuzme")]
    [InlineData("Koşu", "kosu")]
    [InlineData("Şanlıurfa", "sanliurfa")]
    [InlineData("Çanakkale", "canakkale")]
    [InlineData("Düzce", "duzce")]
    [InlineData("Beşiktaş", "besiktas")]
    public void Normalize_FoldsTurkishLettersToAscii(string value, string expected)
    {
        SearchText.Normalize(value).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Normalize_WithoutValue_ReturnsEmpty(string? value)
    {
        SearchText.Normalize(value).Should().BeEmpty();
    }

    [Fact]
    public void Normalize_LeavesAsciiTextAlone()
    {
        SearchText.Normalize("  Basketball  ").Should().Be("basketball");
    }

    /// <summary>
    /// Arama sorgularında kullanılabilmesi için EF'in bu çağrıyı veritabanı
    /// fonksiyonuna çevirmesi şart; çeviremezse sorgu çalışma anında patlar ve
    /// bütün arama uçları çöker. InMemory sağlayıcı LINQ'i bellekte işlettiği
    /// için çeviriyi doğrulamaz — Npgsql'e karşı SQL üretiyoruz, bağlantı
    /// açılmıyor.
    /// </summary>
    [Fact]
    public void Normalize_TranslatesToDatabaseFunction()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=sportner_translation_check")
            .Options;

        using var db = new AppDbContext(options);

        var sql = db.Sports.AsNoTracking()
            .Where(sport => SearchText.Normalize(sport.Name).Contains("yuzme"))
            .ToQueryString();

        sql.Should().Contain("sportner_search_normalize");
    }
}
