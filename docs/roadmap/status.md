# Status — Ne bitti? (anlık özet)

Son güncelleme: 2026-08 (04 background jobs / Worker tamamlandı).

## Tamam

| Alan | Not |
| ---- | --- |
| Domain + Persistence | + `EventReminderDispatches` migration |
| Features 01–09 | Identity → Moderation |
| Auth policies | ActiveUser, CanCreateContent, Moderator, Admin |
| Catalog admin | Sports CRUD |
| Quality hardening | ConfirmAttendance idempotency; counter matrix; handler tests |
| **Workers** | `Identity.Worker` (session/OTP), `Events.Worker` (reminders) — ayrı deploy |
| Storage cleanup | StorageCleanup |
| API rename | `/api/user-profiles` |

## 01 Ops (owner)

| Madde | Durum |
| ----- | ----- |
| RLS SQL (şimdi `EventReminderDispatches` dahil) | SQL Editor’de yeniden çalıştır |
| ModeratorUserIds / AdminUserIds | Pending |
| Client `/api/user-profiles` | Pending |

## 02–04

| Faz | Durum |
| --- | ----- |
| 02 Admin + Catalog | Done |
| 03 Quality hardening | Done |
| 04 Background jobs (Identity + Events workers) | Done |

## Sıradaki

→ [05-signalr-realtime.md](05-signalr-realtime.md)
