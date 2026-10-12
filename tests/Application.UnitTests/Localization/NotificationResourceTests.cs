using System.Globalization;
using FluentAssertions;
using Sportner.Application.Features.Events;
using Sportner.Application.Features.Notifications;
using Sportner.Domain.Common.Enums;
using Sportner.Localization.Resources;

namespace Sportner.Application.UnitTests.Localization;

/// <summary>
/// Bildirim metinleri resx anahtarlarına koda gömülü isimlerle ulaşıyor
/// (örneğin <c>$"EventUpdated_Field_{change}"</c>), yani eksik bir çeviri
/// derlemede yakalanmıyor. <see cref="NotificationActor.Format"/> artık eksik
/// anahtarda istisna atmak yerine anahtarın kendisini döndürüyor — çağıran
/// işlemi düşürmemek için. Bu da eksikliği sessiz hale getirdiğinden, boşluğu
/// bu testler kapatıyor.
/// </summary>
public class NotificationResourceTests
{
    private static readonly string[] Cultures = ["en-US", "tr-TR"];

    [Fact]
    public void EveryEventUpdateField_HasALabelInEveryCulture()
    {
        var missing = new List<string>();

        foreach (var field in Enum.GetValues<EventUpdateField>())
        {
            foreach (var culture in Cultures)
            {
                var key = $"EventUpdated_Field_{field}";
                var value = NotificationsResource.ResourceManager.GetString(
                    key,
                    CultureInfo.GetCultureInfo(culture));

                if (string.IsNullOrWhiteSpace(value))
                {
                    missing.Add($"{key} ({culture})");
                }
            }
        }

        missing.Should().BeEmpty(
            "etiketi olmayan bir alan, bildirimde ham anahtar adı olarak görünür");
    }

    [Theory]
    [InlineData("tr-TR", "başlık")]
    [InlineData("en-US", "title")]
    public void TitleField_IsLocalized(string cultureName, string expected)
    {
        var value = NotificationsResource.ResourceManager.GetString(
            "EventUpdated_Field_Title",
            CultureInfo.GetCultureInfo(cultureName));

        value.Should().Be(expected);
    }

    [Fact]
    public void Format_FallsBackToKey_WhenResourceIsMissing()
    {
        var result = NotificationActor.Format(
            Language.Turkish,
            "ThisKeyDoesNotExist_12345");

        result.Should().Be("ThisKeyDoesNotExist_12345");
    }

    [Fact]
    public void Format_FallsBackToTemplate_WhenArgumentsDoNotMatch()
    {
        // EventUpdated_Body iki yer tutucu bekliyor; eksik argümanla çağrılırsa
        // istisna atmak yerine ham şablonu döndürmeli.
        var result = NotificationActor.Format(Language.Turkish, "EventUpdated_Body");

        result.Should().Contain("{0}");
    }

    [Fact]
    public void Format_FillsPlaceholders_WhenArgumentsMatch()
    {
        var result = NotificationActor.Format(
            Language.Turkish,
            "EventUpdated_Body",
            "Akşam Halı Saha",
            "başlık");

        result.Should().Contain("Akşam Halı Saha").And.Contain("başlık");
        result.Should().NotContain("{0}");
    }
}
