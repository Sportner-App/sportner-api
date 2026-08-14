# 06 — Badge quests (rozet görevleri)

**Amaç:** Anlık award yanında **çok adımlı / süreli görevler** ve ilerleme takibi.  
**Bağımlılık:** [05-badges-depth.md](05-badges-depth.md).  
**Durum:** **Done** (2026-08-14).

---

## Kilitli kararlar (shipped)

| Madde | Karar |
| ----- | ----- |
| Schema | `Quests`, `UserQuests` |
| Status | Active / Completed (+ Expired/Abandoned reserved) |
| Progress | `CurrentValue` / `TargetValue` |
| Trigger | `IQuestProgressTracker.ReportAsync` |
| Reward | Badge only → `IBadgeAwarder.TryAwardAsync` |
| Seasonal | Evergreen (no StartsAt/EndsAt) |
| Claim | Auto-complete |
| Repeatable | No — unique `(UserId, QuestId)` |

---

## API

- `GET /api/quests`
- `GET /api/quests/me`

Notification: `QuestCompleted` (`NotificationType=13`, entity `Quest`).

Migration: `AddQuestsAndUserQuests`.

Docs: `docs/database/29-quests.md`, `30-user-quests.md`, `docs/features/09-quests.md`.

---

## Exit criteria

- [x] Quest list + progress
- [x] ≥3 metric hooks (5 wired)
- [x] Complete → badge award idempotent
- [x] Unique progress
- [x] DB specs + features doc
- [x] status.md → 06 Done

## Sonraki

→ [07-photo-albums.md](07-photo-albums.md)
