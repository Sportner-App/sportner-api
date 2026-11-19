using FluentAssertions;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Notifications;

namespace Sportner.Domain.UnitTests.Notifications;

/// <summary>
/// Varsayılanlar sessizce bir bildirim türünü telefondan kesebiliyor: kullanıcı
/// bildirimi yalnızca uygulamayı açtığında görüyor, kaybolduğunu bile anlamıyor.
/// Beğeni bildirimleri uzun süre böyle kaldı, çünkü varsayılanları doğrulayan
/// hiçbir test yoktu.
/// </summary>
public class NotificationSettingDefaultsTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Kullanıcının açıkça izin vermesi gereken (opt-in) türler. Tanıtım içeren
    /// duyurular izinsiz gönderilmez, bu yüzden varsayılan olarak tamamen kapalı.
    /// </summary>
    private static readonly NotificationType[] OptInTypes = [NotificationType.Announcement];

    public static TheoryData<NotificationType> AllTypes()
    {
        var data = new TheoryData<NotificationType>();

        foreach (var type in Enum.GetValues<NotificationType>())
        {
            data.Add(type);
        }

        return data;
    }

    public static TheoryData<NotificationType> DefaultOnTypes()
    {
        var data = new TheoryData<NotificationType>();

        foreach (var type in Enum.GetValues<NotificationType>().Except(OptInTypes))
        {
            data.Add(type);
        }

        return data;
    }

    /// <summary>
    /// Bir türü varsayılan olarak push'tan çıkarmak bilinçli bir ürün kararı
    /// olmalı — bu testi (OptInTypes listesini) değiştirmeden yapılamaz.
    /// </summary>
    [Theory]
    [MemberData(nameof(DefaultOnTypes))]
    public void CreateDefault_EnablesPush(NotificationType type)
    {
        var setting = NotificationSetting.CreateDefault(Guid.NewGuid(), type, Now);

        setting.PushEnabled.Should().BeTrue();
    }

    [Fact]
    public void CreateDefault_AnnouncementIsOptIn()
    {
        var setting = NotificationSetting.CreateDefault(Guid.NewGuid(), NotificationType.Announcement, Now);

        setting.PushEnabled.Should().BeFalse();
        setting.InAppEnabled.Should().BeFalse();
        setting.EmailEnabled.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(DefaultOnTypes))]
    public void CreateDefault_EnablesInApp(NotificationType type)
    {
        var setting = NotificationSetting.CreateDefault(Guid.NewGuid(), type, Now);

        setting.InAppEnabled.Should().BeTrue();
    }

    /// <summary>E-posta yalnızca sistem duyuruları için; gerisi gürültü olur.</summary>
    [Theory]
    [MemberData(nameof(AllTypes))]
    public void CreateDefault_EnablesEmailOnlyForSystem(NotificationType type)
    {
        var setting = NotificationSetting.CreateDefault(Guid.NewGuid(), type, Now);

        setting.EmailEnabled.Should().Be(type == NotificationType.System);
    }
}
