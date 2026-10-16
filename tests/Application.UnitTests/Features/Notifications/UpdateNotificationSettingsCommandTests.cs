using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Sportner.Application.Features.Notifications.UpdateNotificationSettings;
using Sportner.Application.UnitTests.Infrastructure;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Notifications;

namespace Sportner.Application.UnitTests.Features.Notifications;

/// <summary>
/// Ayarlar ekranı türleri gruplayarak sunuyor, yani tek anahtar onlarca türü
/// birden değiştiriyor. Grubun bütün olarak yazılması şart: yarısı yazılırsa
/// kullanıcı grubu kapalı görürken bir kısmı açık kalır.
/// </summary>
public class UpdateNotificationSettingsCommandTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 3, 2, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_WritesEveryRequestedType()
    {
        await using var db = InMemoryDb.Create();
        var userId = Guid.NewGuid();

        var handler = new UpdateNotificationSettingsCommandHandler(
            db, new TestCurrentUser(userId), new FakeTimeProvider(Now));

        var types = new[]
        {
            NotificationType.PostLiked,
            NotificationType.PostCommented,
            NotificationType.CommentReplied,
        };

        var result = await handler.Handle(
            new UpdateNotificationSettingsCommand(
                types
                    .Select(type => new NotificationSettingUpdate((short)type, true, false, false))
                    .ToList()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var stored = await db.NotificationSettings
            .Where(setting => setting.UserId == userId)
            .ToListAsync();

        stored.Should().HaveCount(types.Length);
        stored.Should().OnlyContain(setting => !setting.PushEnabled && setting.InAppEnabled);
    }

    /// <summary>
    /// Kullanıcı ayarı hiç değiştirmediyse satırı hiç oluşmamış olabilir —
    /// yayıncı satır yokken tür varsayılanına düşüyor. Toplu güncelleme eksik
    /// satırları oluşturmak zorunda, yoksa o türler sessizce atlanır.
    /// </summary>
    [Fact]
    public async Task Handle_CreatesRowsThatDoNotExistYet()
    {
        await using var db = InMemoryDb.Create();
        var userId = Guid.NewGuid();

        db.NotificationSettings.Add(
            NotificationSetting.CreateDefault(userId, NotificationType.PostLiked, Now));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var handler = new UpdateNotificationSettingsCommandHandler(
            db, new TestCurrentUser(userId), new FakeTimeProvider(Now));

        var result = await handler.Handle(
            new UpdateNotificationSettingsCommand(
            [
                new NotificationSettingUpdate((short)NotificationType.PostLiked, true, false, false),
                new NotificationSettingUpdate((short)NotificationType.NewMessage, true, false, false),
            ]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var stored = await db.NotificationSettings
            .Where(setting => setting.UserId == userId)
            .ToListAsync();

        stored.Should().HaveCount(2);
        stored.Should().OnlyContain(setting => !setting.PushEnabled);
    }

    [Fact]
    public async Task Handle_WithoutAuthentication_Fails()
    {
        await using var db = InMemoryDb.Create();

        var handler = new UpdateNotificationSettingsCommandHandler(
            db, new TestCurrentUser(null), new FakeTimeProvider(Now));

        var result = await handler.Handle(
            new UpdateNotificationSettingsCommand(
                [new NotificationSettingUpdate((short)NotificationType.PostLiked, true, true, false)]),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(999)]
    public async Task Handle_WithUnknownType_Fails(short type)
    {
        await using var db = InMemoryDb.Create();

        var handler = new UpdateNotificationSettingsCommandHandler(
            db, new TestCurrentUser(Guid.NewGuid()), new FakeTimeProvider(Now));

        var result = await handler.Handle(
            new UpdateNotificationSettingsCommand(
                [new NotificationSettingUpdate(type, true, true, false)]),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
