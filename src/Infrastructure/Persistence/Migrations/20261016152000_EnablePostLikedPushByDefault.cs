using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sportner.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Beğeni bildirimlerinin push varsayılanı açıldı; mevcut kullanıcıların
    /// satırları da buna çekiliyor.
    ///
    /// Varsayılanı değiştirmek tek başına yetmiyor: NotificationSettings satırları
    /// kayıt sırasında yazıldığı için eski kullanıcılar <c>PushEnabled = false</c>
    /// ile kalıyordu. Uygulamada bildirim ayarları ekranı henüz olmadığından bunu
    /// kendileri açamazlardı.
    ///
    /// Yalnızca eski varsayılanda duran satırlar güncelleniyor. Şu an hepsi öyle —
    /// ayarı kapatmanın bir yolu yoktu — ama koşul niyeti koruyor: ileride bir
    /// kullanıcı bunu bilerek kapatırsa bu migration yeniden çalışsa bile tercihi
    /// ezilmez.
    /// </summary>
    public partial class EnablePostLikedPushByDefault : Migration
    {
        /// <summary>NotificationType.PostLiked</summary>
        private const short PostLiked = 7;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                $"""
                UPDATE "NotificationSettings"
                SET "PushEnabled" = true,
                    "UpdatedAt" = now()
                WHERE "NotificationType" = {PostLiked}
                  AND "PushEnabled" = false;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                $"""
                UPDATE "NotificationSettings"
                SET "PushEnabled" = false,
                    "UpdatedAt" = now()
                WHERE "NotificationType" = {PostLiked}
                  AND "PushEnabled" = true;
                """);
        }
    }
}
