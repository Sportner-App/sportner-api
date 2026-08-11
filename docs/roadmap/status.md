# Status — Ne bitti? (anlık özet)

Son güncelleme: 2026-08 (07.1 badge + 07.2 moderation side effects).

## Tamam

| Alan | Not |
| ---- | --- |
| Domain + Persistence | + outbox, `IsHidden` on Posts/Comments |
| Features 01–09 | Identity → Moderation |
| Auth policies | ActiveUser, CanCreateContent, Moderator, Admin |
| Catalog admin | Sports CRUD |
| Quality hardening | ConfirmAttendance idempotency; counter matrix |
| Workers | Identity / Events (+ marathon sweep) / Notifications |
| SignalR | `/hubs/event-chat` |
| Push delivery | Outbox + LoggingPushSender |
| **Advanced badges** | SPORTS_EXPLORER / EVENT_MASTER / MARATHON_RUNNER / COMMUNITY_HELPER |
| **Moderation effects** | Post/Comment hide; Message redact; Review reported |

## 01 Ops (owner)

| Madde | Durum |
| ----- | ----- |
| RLS SQL (outbox + yeni kolonlar) | SQL Editor’de yeniden çalıştır |
| ModeratorUserIds / AdminUserIds | Pending |
| Client `/api/user-profiles` | Pending |
| Real FCM/APNs | Pending |

## 02–07

| Faz | Durum |
| --- | ----- |
| 02–06 | Done |
| 07.1 Badges | Done |
| 07.2 Moderation side effects | Done |
| 07.3 Direct/Group messaging | Deferred |
| 07.4 Admin badges/reasons | Deferred |

## Sıradaki

→ [08-scale-and-platform.md](08-scale-and-platform.md) veya 7.3 Direct
