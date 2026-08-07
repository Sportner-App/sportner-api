# 01 — Mevcut durum (tamamlananlar)

Son güncelleme: 2026-08.

## Katmanlar

| Katman | Durum | Not |
| ------ | ----- | --- |
| Domain (26 entity) | Tamam | Aggregate + invariant’lar |
| Persistence + migrations | Tamam | `InitialCreate` + `RenameProfilesToUserProfiles` |
| Application foundation | Tamam | CQRS, Result, FluentValidation, Mapster |
| Seed (Sports / Badges / ReportReasons) | Tamam | Startup’ta idempotent |
| Demo data | Tamam | Tekrar çalıştırılmaz (skip) |

## Feature modülleri

| Modül | Durum | Ana yüzey |
| ----- | ----- | --------- |
| 01 Identity | Tamam | OTP/JWT, `/api/user-profiles`, devices, sessions, sports, locations |
| 02 Catalog | Tamam (read) | Aktif spor listesi / slug |
| 03 Events | Tamam | Lifecycle, başvuru, waitlist, attendance, discovery |
| 04 Messaging | Tamam (REST) | Event chat, cursor messages, media, edit/redact |
| 05 Reviews | Tamam | Create/update/list/reviewable + rating cache |
| 06 Social | Tamam | Friendships, posts, likes, comments, feed |
| 07 Notifications | Tamam (in-app) | Inbox + settings; publisher diğer modüllerde |
| 08 Gamification | Tamam (FIRST_*) | Catalog + my/user badges + `IBadgeAwarder` |
| 09 Moderation | Tamam (temel) | Reasons, create/mine, moderator queue |
| 10 Cross-cutting | Kısmen | ActiveUser / CanCreateContent / Moderator; storage cleanup |

## Önemli altyapı parçaları

- `INotificationPublisher` → in-app (Events / Social / Messaging / Badges)
- `IBadgeAwarder` → idempotent FIRST_* ödülleri
- `StorageCleanup` → commit sonrası best-effort storage silme
- Authorization: default `ActiveUser`, içerik için `CanCreateContent`, rapor queue için `Moderator` allow-list
- Tablo/isim: `Profiles` → `UserProfiles`, API `/api/user-profiles`

## Test durumu (yaklaşık)

- Domain unit + Application unit + API integration mevcut
- Handler coverage tüm yüzeylerde eşit değil; kritik path’ler kısmen kapsanıyor

## Bilinçli olarak MVP dışında bırakılanlar

Bunlar “eksik bug” değil; ürün kararı ile ertelendi — detay [04-advanced-next.md](04-advanced-next.md).

- SignalR / typing / presence
- Push & email workers
- Direct / Group DM
- PostGIS discovery
- Admin CRUD (Sports / Badges / ReportReasons)
- İleri badge kuralları (SPORTS_EXPLORER, EVENT_MASTER, …)
