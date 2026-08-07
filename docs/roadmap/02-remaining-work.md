# 02 — Kalan işler (yapılacaklar)

Öncelik: **P0** yakında / prod öncesi · **P1** MVP+ · **P2** ileri faz.

## P0 — Yakın / ops / prod kapısı

| # | İş | Neden | Kaynak |
| - | -- | ----- | ------ |
| 1 | Supabase RLS + Data API kilidi | Public API ile tablo yazımı riski | [00-prerequisites](../features/00-prerequisites.md) |
| 2 | Secrets’ı tracked config’ten çıkarma + rotate | `appsettings.*.json` içinde connection/JWT/service_role var | [configuration](../configuration.md), [10-cross-cutting](../features/10-cross-cutting.md) |
| 3 | `Authorization:ModeratorUserIds` gerçek moderator id’leri | Rapor queue şu an boş allow-list ile 403 | appsettings + 09 Moderation |
| 4 | Mobil client route güncellemesi | `/api/profiles` → `/api/user-profiles` | UserProfile rename |

> Not: DB migration’lar (InitialCreate + RenameProfilesToUserProfiles) hedef Supabase’e uygulandıysa P0’dan düşer; yeni ortamda tekrar `dotnet ef database update` gerekir.

## P1 — Ürün boşlukları (MVP’yi kalınlaştırır)

| # | İş | Not |
| - | -- | --- |
| 1 | Admin policy (`Admin`) + Sports admin CRUD | Catalog write deferred |
| 2 | Report side-effects: Post/Comment/User hide-suspend kuralları | Şu an Review `MarkAsReported` var; diğer entity’ler deferred |
| 3 | İleri badge kuralları | `SPORTS_EXPLORER`, `EVENT_MASTER`, `MARATHON_RUNNER`, `COMMUNITY_HELPER` — eşikler netleştirilmeli |
| 4 | Event reminder bildirimi | Job olmadan REST tarafında tetik yok |
| 5 | Counter reconciliation job (gecelik) | Drift riskine karşı doğrulama |
| 6 | Storage orphan GC job | Best-effort delete başarısız olursa temizleyen worker |
| 7 | Handler / integration test borçları | Özellikle Events, Social, Moderation happy+fail path |

## P2 — Platform (ayrı faz)

Ayrıntı: [04-advanced-next.md](04-advanced-next.md).

- Background jobs host (Hangfire / Quartz / worker)
- SignalR (event chat push, typing, presence)
- Push / email delivery
- Direct / Group messaging
- PostGIS / advanced discovery
- Admin badge & report-reason CRUD UI/API

## Modül bazlı “açık checkbox” özeti

| Modül | Hâlâ açık |
| ----- | --------- |
| 00 Prerequisites | RLS / Data API (manuel) |
| 02 Catalog | Admin mutate |
| 04 Messaging | SignalR, Direct/Group, Location message |
| 07 Notifications | Push/email workers |
| 08 Gamification | Admin CRUD + non-FIRST_* rules |
| 09 Moderation | Non-review target side effects; Admin reasons CRUD |
| 10 Cross-cutting | Jobs, SignalR, secrets hygiene |

## Önerilen sprint kesitleri

1. **Ops sprint:** RLS + secrets + moderator allow-list + client route sync  
2. **Admin sprint:** `Admin` policy + Sports CRUD  
3. **Jobs sprint:** host seçimi + session/OTP cleanup + event reminder  
4. **Realtime sprint:** SignalR event chat (REST kurallarını bozmadan)
