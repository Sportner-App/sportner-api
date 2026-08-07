# 04 — İleri seviye (sonraki fazlar)

MVP feature yüzeyi (01–09) bittikten sonra ürünü “production-grade” yapan adımlar.

## Faz A — Background jobs

Host kararı (Hangfire / Quartz / ayrı worker) Infrastructure’da kalır; Application sadece contract görür.

| Job | Amaç |
| --- | ---- |
| Expired session cleanup | ~90 gün retention |
| OTP cleanup | Kullanılmamış / süresi dolmuş OTP |
| Event reminders | `EventReminder` bildirimi |
| Badge rule sweeps | FIRST_* dışı kurallar |
| Counter reconciliation | Cache ↔ source doğrulama |
| Push/email dispatch | Notification outbox |
| Storage orphan GC | Commit-sonrası başarısız delete’ler |

## Faz B — Realtime (SignalR)

| Hub / özellik | Not |
| ------------- | --- |
| Event chat push | REST yazdıktan sonra push; domain kuralları REST’te kalır |
| Typing | Ephemeral |
| Presence | Device / `LastSeen` |
| In-app notification badge | Mobil push ile birleşebilir |

## Faz C — Messaging genişleme

- Direct / Group conversation factory
- `MessageType.Location`
- Typing + read receipts (ürün kararı)

## Faz D — Admin & moderation derinliği

- `Admin` policy (allow-list → roles)
- Sports / Badges / ReportReasons CRUD
- Report resolve sonrası Post/Comment/User/Message aksiyon matrisi (hide, suspend, ban)
- Moderator dashboard ihtiyaçları (filtre, atama, SLA)

## Faz E — Gamification derinliği

Önce **eşik kurallarını** yaz (koddan önce):

| Code | Örnek kural (netleştir) |
| ---- | ----------------------- |
| `SPORTS_EXPLORER` | N distinct user sports / played sports |
| `EVENT_MASTER` | N attended events |
| `MARATHON_RUNNER` | Streak veya volume |
| `COMMUNITY_HELPER` | Helpful report / comment eşiği |

Sonra job veya mutation hook ile `IBadgeAwarder.TryAwardAsync`.

## Faz F — Discovery & performans

- PostGIS / radius search
- Feed ve discovery için index + pagination stress
- Hot path’te raw `COUNT(*)` yok (counter cache devam)
- Phase 10: load / security test suite

## Faz G — Platform olgunluğu

| Konu | Hedef |
| ---- | ----- |
| Secrets | Env / secret store; history rotate |
| Observability | Structured logs + correlation; opsiyonel APM |
| CI | build + test + (opsiyonel) migration dry-run |
| SMS | LoggingSmsSender → gerçek provider |
| Rate limit | OTP / login brute-force koruması |

## Bilinçli non-goals (erken ürün)

- Turnuva / takım / ödeme modülleri
- Full admin backoffice UI (API önce)
- Client’ın Supabase Data API ile business tablo kullanımı

---

Bu fazlar birbirini kilitlemez: A (jobs) olmadan B (SignalR) başlanabilir; ama push/email ve reminder için A şarttır.
